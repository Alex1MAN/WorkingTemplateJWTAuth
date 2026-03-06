using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace JWTAuthTemplate.Application.Services
{
    public class MinioService : IMinioService
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
    }
}
