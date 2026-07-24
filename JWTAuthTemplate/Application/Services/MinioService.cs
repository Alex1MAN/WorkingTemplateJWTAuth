using ExcelDataReader;
using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Infrastructure.Database;
using JWTAuthTemplate.Models.Identity;
using JWTAuthTemplate.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Elchwinkel.Spc;

namespace JWTAuthTemplate.Application.Services;

public class MinioService : IMinioService
{
    private readonly MinioClient _minioClient;
    private readonly Context _context;

    public MinioService(IOptions<MinioSettingsDTO> minioSettings, Context context)
    {
        _minioClient = (MinioClient?)new MinioClient()
            .WithEndpoint(minioSettings.Value.Endpoint)
            .WithCredentials(minioSettings.Value.AccessKey, minioSettings.Value.SecretKey)
            .Build() ?? throw new InvalidOperationException("MinioClient initialization failed");
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

    public async Task<string> GetObjectETagAsync(string bucketName, string objectName)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            var objectStat = await _minioClient.StatObjectAsync(statArgs);
            return objectStat.ETag;
        }
        catch (Exception ex)
        {
            throw new Exception($"Couldn't get the ETAG for {objectName}", ex);
        }
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

    public async Task UploadFileAsync(string bucketName, string objectName, Stream fileStream, long length)
    {
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(length)
            .WithContentType("application/octet-stream"));
    }

    public async Task<UploadResultDTO> UploadFilesAsync(string bucketName, List<IFormFile> filesData)
    {
        if (filesData == null || !filesData.Any())
        {
            throw new ArgumentException("No files provided for upload");
        }

        var existingKeys = await _context.UserReferencesInMinio
            .Where(ur => ur.UserId == bucketName)
            .Select(ur => ur.FileName + "|||" + ur.FileExtension)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingKeys);

        var result = new UploadResultDTO
        {
            BucketName = bucketName,
            References = new List<FileReferenceDTO>()
        };

        foreach (var fileData in filesData)
        {
            if (fileData == null || fileData.Length == 0)
            {
                continue;
            }

            var fileName = fileData.FileName;
            var fileExtension = Path.GetExtension(fileName).Replace(".", "");

            var fileKey = fileName + "|||" + fileExtension;
            if (existingSet.Contains(fileKey))
            {
                throw new ArgumentException($"File '{fileName}' already exists in the database");
            }

            using var memoryStream = fileData.OpenReadStream();
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(memoryStream)
                .WithObjectSize(fileData.Length)
                .WithContentType("application/octet-stream"));

            var etag = await GetObjectETagAsync(bucketName, fileName);

            var reference = new FileReferenceDTO
            {
                FileName = fileName,
                FileExtension = fileExtension,
                FileReferenceMinio = etag
            };
            result.References.Add(reference);
        }

        result.Count = result.References.Count;
        return result;
    }

    public async Task<FileReferenceDTO> AddReferenceAsync(string userId, string fileName, string fileExtension, string fileReferenceMinio)
    {
        var reference = new UserReferencesInMinio
        {
            UserId = userId,
            FileName = fileName,
            FileExtension = fileExtension,
            FileReferenceMinio = fileReferenceMinio
        };

        _context.UserReferencesInMinio.Add(reference);
        await _context.SaveChangesAsync();

        return new FileReferenceDTO
        {
            FileName = fileName,
            FileExtension = fileExtension,
            FileReferenceMinio = fileReferenceMinio
        };
    }

    public async Task<List<FileReferenceDTO>> GetReferencesByUserIdAsync(string userId)
    {
        var references = await _context.UserReferencesInMinio
            .Where(ur => ur.UserId == userId)
            .Select(ur => new FileReferenceDTO
            {
                FileName = ur.FileName ?? string.Empty,
                FileExtension = ur.FileExtension ?? string.Empty,
                FileReferenceMinio = ur.FileReferenceMinio ?? string.Empty
            })
            .ToListAsync();

        return references;
    }

    public async Task<string> GetFileUrlAsync(string bucketName, string objectName)
    {
        var stream = await GetFileAsync(bucketName, objectName);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
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

        var labels = new List<string>();
        for (int col = 0; col < table.Columns.Count; col++)
            labels.Add(table.Rows[0][col]?.ToString() ?? "");

        var sb = new StringBuilder();
        sb.Append("{\n  \"labels\": [");
        for (int i = 0; i < labels.Count; i++)
        {
            sb.Append($"\"{EscapeJson(labels[i])}\"");
            if (i < labels.Count - 1)
                sb.Append(", ");
        }
        sb.Append("],\n  \"values\": [\n    [\n");

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

        for (int row = 1; row < table.Rows.Count; row++)
        {
            sb.Append("      {");
            for (int i = 0; i < filteredCols.Count; i++)
            {
                int col = filteredCols[i];
                string label = EscapeJson(labels[col]);
                string cellStr = table.Rows[row][col]?.ToString() ?? "";

                if (double.TryParse(cellStr, out double cellValue))
                {
                    if (cellValue < minY)
                        cellValue = minY;
                    else if (cellValue > maxY)
                        cellValue = maxY;

                    cellStr = cellValue.ToString(System.Globalization.CultureInfo.CurrentCulture);
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

        byte[] spcBytes = memoryStream.ToArray();
        var spc = SpcReader.Read(spcBytes);

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

        return sb.ToString();
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return s.Replace("\\", "\\\\")
               .Replace("\"", "\\\"")
               .Replace("\n", "\\n")
               .Replace("\r", "\\r");
    }
}
