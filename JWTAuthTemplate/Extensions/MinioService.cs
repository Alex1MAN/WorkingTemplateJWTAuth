using ExcelDataReader;
using JWTAuthTemplate.DTO.Identity;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Elchwinkel.Spc;

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

        // Работа с эксель-файлом
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

            // Формируем JSON по строкам, начиная со второй (index 1)
            for (int row = 1; row < table.Rows.Count; row++)
            {
                sb.Append("      {");
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    string label = EscapeJson(labels[col]);
                    string cellValue = EscapeJson(table.Rows[row][col]?.ToString() ?? "");
                    sb.Append($"\"{label}\": \"{cellValue}\"");
                    if (col < table.Columns.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
                if (row < table.Rows.Count - 1)
                    sb.Append(",\n");
                else
                    sb.Append("\n");
            }

            sb.Append("    ]\n  ]\n}");
            return sb.ToString();
        }

        // Работа с эксель-файлом
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

            // Фильтруем столбцы по x1 и x2 (включительно)
            var filteredCols = new List<int>();
            double minX = Math.Min(inputX1, inputX2);
            double maxX = Math.Max(inputX1, inputX2);
            for (int i = 0; i < labelDoubles.Count; i++)
            {
                if (labelDoubles[i].HasValue)
                {
                    double val = labelDoubles[i].Value;
                    if (val >= minX && val <= maxX)
                        filteredCols.Add(i);
                }
            }

            double minY = Math.Min(inputY1, inputY2);
            double maxY = Math.Max(inputY1, inputY2);

            var sb = new StringBuilder();
            sb.Append("{\n  \"labels\": [");
            for (int i = 0; i < filteredCols.Count; i++)
            {
                sb.Append($"\"{EscapeJson(labels[filteredCols[i]])}\"");
                if (i < filteredCols.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("],\n  \"values\": [\n    [\n");

            // Формируем JSON по строкам (начиная со второй строки — index 1)
            for (int row = 1; row < table.Rows.Count; row++)
            {
                sb.Append("      {");
                for (int i = 0; i < filteredCols.Count; i++)
                {
                    int col = filteredCols[i];
                    string label = EscapeJson(labels[col]);
                    string cellStr = table.Rows[row][col]?.ToString() ?? "";

                    // Пытаемся преобразовать в double
                    if (double.TryParse(cellStr, out double cellValue))
                    {
                        // Корректируем значение по границам y
                        if (cellValue < minY)
                            cellValue = minY;
                        else if (cellValue > maxY)
                            cellValue = maxY;

                        // Используем формат с запятой, если нужно (например, "0,055"), 
                        // иначе ToString(CultureInfo.InvariantCulture) для точки
                        // Здесь возьмём текущую культуру (можно заменить при необходимости)
                        cellStr = cellValue.ToString(System.Globalization.CultureInfo.CurrentCulture);
                    }
                    else
                    {
                        // Если не число, можно оставить пустую строку или оригинал
                        // Здесь оставим оригинал без изменений
                    }

                    sb.Append($"\"{label}\": \"{EscapeJson(cellStr)}\"");
                    if (i < filteredCols.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
                if (row < table.Rows.Count - 1)
                    sb.Append(",\n");
                else
                    sb.Append("\n");
            }

            sb.Append("    ]\n  ]\n}");
            return sb.ToString();
        }


        // Работа с SPC-файлом
        public async Task<string> GetSPCFileContentAsJson(string bucketName, string objectName)
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

            string tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFilePath))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(fileStream);
            }
            var spc = SpcReader.Read(tempFilePath);

            var sb = new StringBuilder();
            var labels = new List<string> { spc.XUnit.Name, spc.YUnit.Name };
            var xValues = spc.Spectra[0].X;
            var yValues = spc.Spectra[0].Y;
            sb.Append("{\n  \"labels\": [");
            for (int i = 0; i < labels.Count; i++)
            {
                sb.Append($"\"{EscapeJson(labels[i])}\"");
                if (i < labels.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("],\n  \"values\": [\n    [\n");
            for (int i = 0; i < xValues.Length; i++)
            {
                sb.Append("      {");
                sb.Append($"\"{EscapeJson(labels[0])}\": \"{xValues[i]}\"");
                sb.Append(", ");
                sb.Append($"\"{EscapeJson(labels[1])}\": \"{yValues[i]}\"");
                sb.Append("}");
                if (i < xValues.Length - 1)
                    sb.Append(",\n");
                else
                    sb.Append("\n");
            }
            sb.Append("    ]\n  ]\n}");
            File.Delete(tempFilePath);

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
