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

            // ms = memoryStream

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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

        // Метод для экранирования строк в JSON (кавычки, обратные слэши и т.п.)
        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
