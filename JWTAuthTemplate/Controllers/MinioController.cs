using JWTAuthTemplate.Context;
using JWTAuthTemplate.Extensions;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;

using System.Data;
using ExcelDataReader;
using Newtonsoft.Json;

using Minio.DataModel.Args;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace JWTAuthTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MinioController : ControllerBase
    {
        private readonly MinioService _minioService;
        private readonly ApplicationDbContext _context;

        public MinioController(MinioService minioService, ApplicationDbContext context)
        {
            _minioService = minioService;
            _context = context;
        }

        [HttpPost("create-bucket")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            await _minioService.CreateBucketAsync(bucketName);
            return Ok($"Bucket {bucketName} created.");
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(
            [FromForm] string bucketName,
            [FromForm] string fileName,
            [FromForm] IFormFile fileData)
        {
            if (fileData == null || fileData.Length == 0)
            {
                return BadRequest("File data cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name cannot be null or empty.");
            }
            // Создаем временный файл
            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await fileData.CopyToAsync(stream);
                }
                // Вызываем метод загрузки по пути временного файла
                await _minioService.UploadFileAsync(bucketName, fileName, tempFilePath);
            }
            finally
            {
                // Удаляем временный файл
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch (Exception deleteEx)
                    {
                        Console.Error.WriteLine($"Ошибка удаления временного файла: {deleteEx.Message}");
                    }
                }
            }
            return Ok($"File {fileName} uploaded to bucket {bucketName}.");
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }
            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                // Вызываем метод загрузки по пути временного файла
                await _minioService.UploadFileAsync("44444", "test13", tempFilePath);
            }
            finally
            {
                // Удаляем временный файл
                if (System.IO.File.Exists(tempFilePath))
                {
                    //System.IO.File.Delete(tempFilePath);
                }
            }
            return Ok("File uploaded successfully.");
        }

        // Тестовый метод ниже
        [HttpPost("test-upload-file-direct-reference")]
        public async Task<IActionResult> TestUploadFile(string bucketName, string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File {Path.GetFileName(filePath)} not found.");
            }
            await _minioService.UploadFileAsync(bucketName, Path.GetFileName(filePath), filePath);
            
            return Ok($"File {Path.GetFileName(filePath)} uploaded to bucket {bucketName}.");
        }

        [HttpPost]
        public async Task<IActionResult> AddReferenceInPostgre(UserReferencesInMinio userReferencesInMinio)
        {
            _context.UserReferencesInMinio.Add(userReferencesInMinio);
            await _context.SaveChangesAsync();

            return CreatedAtAction("AddReferenceInPostgre", new { id = userReferencesInMinio.Id }, userReferencesInMinio);
        }

        [HttpGet("files/{userId}")]
        public async Task<IActionResult> GetFileReferencesByUserId(string userId)
        {
            var fileReferences = await _context.UserReferencesInMinio
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.FileName,
                    u.FileExtension,
                    u.FileReferenceMinio
                })
                .ToListAsync();

            return Ok(fileReferences);
        }

        /*[HttpGet("get-file")]
        public async Task<IActionResult> GetFile(string bucketName, string objectName)
        {
            var fileUrl = await _minioService.GetFileAsync(bucketName, objectName);
            return Ok(new { Url = fileUrl });
        }*/


        [HttpGet("{bucketName}/{fileName}")]
        public async Task<IActionResult> GetFileContent(string bucketName, string fileName)
        {
            try
            {
                var stream = await _minioService.GetFileAsync(bucketName, fileName);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("get-all-table")]
        public async Task<IActionResult> GetTableFromExcel(string bucketName, string fileName)
        {
            try
            {
                var resultTable = await _minioService.GetExcelFileContentAsJson(bucketName, fileName);
                return Ok(resultTable);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("get-table-with-limits")]
        public async Task<IActionResult> GetTableFromExcelWithLimits(string bucketName, string fileName, double inputX1, double inputX2, double inputY1, double inputY2)
        {
            try
            {
                var resultTable = await _minioService.GetExcelFileContentAsJsonWithLimits(bucketName, fileName, inputX1, inputX2, inputY1, inputY2);
                return Ok(resultTable);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
