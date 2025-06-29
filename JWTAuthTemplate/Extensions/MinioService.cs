using ExcelDataReader;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Migrations;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace JWTAuthTemplate.Extensions
{
    public class MinioService
    {
        private readonly MinioClient _minioClient;

        public MinioService(IOptions<MinioSettingsDTO> minioSettings)
        {
            // Создаем MinioClient с использованием конфигурации
            _minioClient = (MinioClient?)new MinioClient()
                .WithEndpoint(minioSettings.Value.Endpoint)
                .WithCredentials(minioSettings.Value.AccessKey, minioSettings.Value.SecretKey)
                .Build();
        }

        public async Task CreateBucketAsync(string bucketName)
        {
            var args = new BucketExistsArgs().WithBucket(bucketName);
            bool found = await _minioClient.BucketExistsAsync(args);
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }
        }

        public async Task UploadFileAsync(string bucketName, string objectName, string filePath)
        {
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFileName(filePath)
                .WithObjectSize(new FileInfo(filePath).Length)
                .WithContentType("application/octet-stream"));
        }

        public async Task<Stream> GetFileAsync(string bucketName, string objectName)
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });
            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<string> GetExcelFileContentAsJson(string bucketName, string objectName)
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });
            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(memoryStream);
            var result = reader.AsDataSet();
            var table = result.Tables[0];

            // Считаем labels из первой строки
            var labels = new List<string>();
            for (int col = 0; col < table.Columns.Count; col++)
                labels.Add(table.Rows[0][col]?.ToString() ?? "");

            // 2. values: массив массивов, где каждый подмассив — объект {label: [значения]}
            /*
            var values = new List<List<Dictionary<string, List<string>>>>();
            var valueRow = new List<Dictionary<string, List<string>>>();
            for (int col = 0; col < table.Columns.Count; col++)
            {
                var colValues = new List<string>();
                for (int row = 1; row < table.Rows.Count; row++)
                {
                    colValues.Add(table.Rows[row][col]?.ToString() ?? "");
                }
                valueRow.Add(new Dictionary<string, List<string>> { { labels[col], colValues } });
            }
            values.Add(valueRow);
            var resultObj = new
            {
                labels,
                values
            };
            return JsonConvert.SerializeObject(resultObj, Formatting.Indented);
            */

            // Начинаем строить JSON вручную (как хотят на фронте)
            var sb = new StringBuilder();
            sb.Append("{\n  \"labels\": [");
            // Добавляем labels в JSON
            for (int i = 0; i < labels.Count; i++)
            {
                sb.Append($"\"{EscapeJson(labels[i])}\"");
                if (i < labels.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("],\n  \"values\": [\n    [\n");
            // Для каждого столбца формируем объект с повторяющимися ключами
            for (int col = 0; col < table.Columns.Count; col++)
            {
                sb.Append("      {");
                string label = EscapeJson(labels[col]);
                // Добавляем повторяющиеся ключи с их значениями из каждой строки
                for (int row = 1; row < table.Rows.Count; row++)
                {
                    string cellValue = EscapeJson(table.Rows[row][col]?.ToString() ?? "");
                    sb.Append($"\"{label}\": \"{cellValue}\"");
                    if (row < table.Rows.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
                if (col < table.Columns.Count - 1)
                    sb.Append(",\n");
                else
                    sb.Append("\n");
            }
            sb.Append("    ]\n  ]\n}");
            return sb.ToString();
        }

        public async Task<string> GetExcelFileContentAsJsonWithLimits(string bucketName, string objectName, double inputX1, double inputX2, double inputY1, double inputY2)
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });
            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(memoryStream);
            var result = reader.AsDataSet();
            var table = result.Tables[0];

            // Считаем labels из первой строки и пытаемся преобразовать в double
            var labels = new List<string>();
            var labelDoubles = new List<double?>();
            for (int col = 0; col < table.Columns.Count; col++)
            {
                string labelStr = table.Rows[0][col]?.ToString() ?? "";
                labels.Add(labelStr);

                if (double.TryParse(labelStr, out double d))
                    labelDoubles.Add(d);
                else
                    labelDoubles.Add(null);
            }

            // Фильтруем столбцы по x1 и x2 (включительно), только те, где label можно преобразовать в double и входит в диапазон
            var filteredCols = new List<int>();
            for (int i = 0; i < labelDoubles.Count; i++)
            {
                if (labelDoubles[i].HasValue)
                {
                    double val = labelDoubles[i].Value;
                    if (val >= Math.Min(inputX1, inputX2) && val <= Math.Max(inputX1, inputX2))
                        filteredCols.Add(i);
                }
            }

            // Начинаем строить JSON
            var sb = new StringBuilder();
            sb.Append("{\n  \"labels\": [");

            // Добавляем labels выбранных столбцов
            for (int i = 0; i < filteredCols.Count; i++)
            {
                sb.Append($"\"{EscapeJson(labels[filteredCols[i]])}\"");
                if (i < filteredCols.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("],\n  \"values\": [\n    [\n");

            // Для каждого выбранного столбца формируем объект с повторяющимися ключами,
            // но только для тех значений, которые попадают в диапазон y1..y2 (по значению ячейки)
            for (int colIndex = 0; colIndex < filteredCols.Count; colIndex++)
            {
                int col = filteredCols[colIndex];
                sb.Append("      {");
                string label = EscapeJson(labels[col]);

                // Собираем значения, удовлетворяющие условию y1 <= value <= y2
                var filteredValues = new List<string>();
                for (int row = 1; row < table.Rows.Count; row++) // строки данных без заголовка
                {
                    string cellStr = table.Rows[row][col]?.ToString() ?? "";
                    if (double.TryParse(cellStr, out double cellValue))
                    {
                        if (cellValue >= Math.Min(inputY1, inputY2) && cellValue <= Math.Max(inputY1, inputY2))
                            filteredValues.Add(cellStr);
                    }
                    else
                    {
                        // Если не число — игнорируем
                    }
                }

                // Если нет значений — можно либо пропустить, либо вернуть пустой объект с ключом
                // Здесь вернём пустой объект с ключом и без значений
                for (int i = 0; i < filteredValues.Count; i++)
                {
                    sb.Append($"\"{label}\": \"{EscapeJson(filteredValues[i])}\"");
                    if (i < filteredValues.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
                if (colIndex < filteredCols.Count - 1)
                    sb.Append(",\n");
                else
                    sb.Append("\n");
            }

            sb.Append("    ]\n  ]\n}");

            return sb.ToString();
        }

        // Метод для экранирования строк в JSON (кавычки, обратные слэши и т.п.)
        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
