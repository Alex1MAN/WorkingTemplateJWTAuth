using Microsoft.AspNetCore.Mvc;
using Minio;
using JWTAuthTemplate.Extensions;
using System.IO;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace JWTAuthTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MinioController : ControllerBase
    {
        private readonly MinioService _minioService;

        public MinioController(MinioService minioService)
        {
            _minioService = minioService;
        }

        [HttpPost("create-bucket")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            await _minioService.CreateBucketAsync(bucketName);
            return Ok($"Bucket {bucketName} created.");
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(string bucketName, string fileName, [FromBody] byte[] fileData)
        {
            if (fileData == null || fileData.Length == 0)
            {
                return BadRequest("File data cannot be null or empty.");
            }
            // Создаем временный файл
            var tempFilePath = Path.GetTempFileName();
            try
            {
                await System.IO.File.WriteAllBytesAsync(tempFilePath, fileData);
                // Вызываем метод загрузки по пути временного файла
                await _minioService.UploadFileAsync(bucketName, fileName, tempFilePath);
            }
            finally
            {
                // Удаляем временный файл
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
            return Ok($"File {fileName} uploaded to bucket {bucketName}.");
        }
        /*
        // Тестовый метод ниже
        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(string bucketName, string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File {Path.GetFileName(filePath)} not found.");
            }
            await _minioService.UploadFileAsync(bucketName, Path.GetFileName(filePath), filePath);
            return Ok($"File {Path.GetFileName(filePath)} uploaded to bucket {bucketName}.");
        }
        */

        [HttpGet("get-file")]
        public async Task<IActionResult> GetFile(string bucketName, string objectName)
        {
            var fileUrl = await _minioService.GetFileAsync(bucketName, objectName);
            return Ok(new { Url = fileUrl });
        }
    }
}
