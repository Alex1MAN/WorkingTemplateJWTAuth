using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Migrations;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
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

        public async Task<IResult> GetFileAsync(string bucketName, string objectName)
        {
            /*var presignedUrl = await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithExpiry(3600)
                .WithObject(objectName));
            return presignedUrl;*/

            var memoryStream = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async stream => await stream.CopyToAsync(memoryStream))
            );


            memoryStream.Position = 0;
            return Results.File(memoryStream, "application/octet-stream", objectName);
        }
    }
}
