using JWTAuthTemplate.Context;
using JWTAuthTemplate.Extensions;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace JWTAuthTemplate.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MinioController : ControllerBase
    {
        private readonly MinioService _minioService;
        private readonly ApplicationDbContext _context;

        public MinioController(MinioService minioService, ApplicationDbContext context)
        {
            _minioService = minioService;
            _context = context;
        }

        [HttpPost("CreateBucket")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            await _minioService.CreateBucketAsync(bucketName);
            return Ok($"Bucket {bucketName} created.");
        }

        [HttpPost("UploadFileUpdateReference")]
        public async Task<IActionResult> UploadFileUpdateReference(
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
            //return Ok($"File {fileName} uploaded to bucket {bucketName}.");

            // Ниже добавляем ссылку MinIO в PostgreSQL
            var streamUrl = await _minioService.GetFileAsync(bucketName, fileName);
            streamUrl.Position = 0;
            string fileUrl = "def value";
            using (StreamReader reader = new StreamReader(streamUrl))
            {
                fileUrl = await reader.ReadToEndAsync();
            }

            var reference = new UserReferencesInMinio
            {
                UserId = bucketName,
                FileName = fileName,
                FileExtension = Path.GetExtension(fileName).Replace(".", ""),
                FileReferenceMinio = fileUrl
            };

            _context.UserReferencesInMinio.Add(reference);
            await _context.SaveChangesAsync();

            return Ok($"File {fileName} uploaded to bucket {bucketName} and reference updated in Postgre.");
        }

        // Основной метод загрузки файлов, метод с 1 файлом (UploadFileUpdateReference) не нужен
        [HttpPost("UploadSeveralFilesAndUpdateReferencesInPostgre")]
        public async Task<IActionResult> UploadSeveralFilesAndUpdateReferencesInPostgre(
            [FromForm] string bucketName,
            [FromForm] List<IFormFile> filesData)
        {
            if (filesData == null || !filesData.Any())
            {
                return BadRequest("No files provided for upload.");
            }
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be null or empty.");
            }

            foreach (var fileData in filesData)
            {
                if (fileData == null || fileData.Length == 0)
                {
                    continue; // Skip empty files
                }

                var fileName = fileData.FileName;
                // Create temp file
                var tempFilePath = Path.GetTempFileName();
                try
                {
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await fileData.CopyToAsync(stream);
                    }
                    // Upload file to MinIO
                    await _minioService.UploadFileAsync(bucketName, fileName, tempFilePath);

                    // Get the file reference URL from MinIO
                    var streamUrl = await _minioService.GetFileAsync(bucketName, fileName);
                    streamUrl.Position = 0;
                    string fileUrl;
                    using (StreamReader reader = new StreamReader(streamUrl))
                    {
                        fileUrl = await reader.ReadToEndAsync();
                    }

                    // Store reference in DB
                    var reference = new UserReferencesInMinio
                    {
                        UserId = bucketName,
                        FileName = fileName,
                        FileExtension = Path.GetExtension(fileName).Replace(".", ""),
                        FileReferenceMinio = fileUrl
                    };
                    _context.UserReferencesInMinio.Add(reference);
                }
                finally
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(tempFilePath);
                        }
                        catch (Exception deleteEx)
                        {
                            Console.Error.WriteLine($"Error deleting temp file: {deleteEx.Message}");
                        }
                    }
                }
            }

            // Save all references at once
            await _context.SaveChangesAsync();

            return Ok($"Uploaded {filesData.Count} files to bucket {bucketName} and updated references in database.");
        }


        // Новая версия основного метода без временных файлов
        public async Task<IActionResult> UploadSeveralFilesAndUpdateReferencesInPostgre2(
            [FromForm] string bucketName,
            [FromForm] List<IFormFile> filesData)
        {
            if (filesData == null || !filesData.Any())
            {
                return BadRequest("No files provided for upload.");
            }
            if (string.IsNullOrEmpty(bucketName))
            {
                return BadRequest("Bucket name cannot be null or empty.");
            }

            var references = new List<UserReferencesInMinio>();

            foreach (var fileData in filesData)
            {
                if (fileData == null || fileData.Length == 0)
                {
                    continue; // Skip empty files
                }

                var fileName = fileData.FileName;
                var fileExtension = Path.GetExtension(fileName).Replace(".", "");

                try
                {
                    // Upload file to MinIO directly from memory stream
                    using var memoryStream = fileData.OpenReadStream();
                    await _minioService.UploadFileAsync2(bucketName, fileName, memoryStream, fileData.Length);

                    // Get the file reference URL from MinIO
                    var streamUrl = await _minioService.GetFileAsync(bucketName, fileName);
                    streamUrl.Position = 0;
                    string fileUrl;
                    using (StreamReader reader = new StreamReader(streamUrl))
                    {
                        fileUrl = await reader.ReadToEndAsync();
                    }

                    // Prepare reference for batch insert
                    var reference = new UserReferencesInMinio
                    {
                        UserId = bucketName,
                        FileName = fileName,
                        FileExtension = fileExtension,
                        FileReferenceMinio = fileUrl
                    };
                    references.Add(reference);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                    Console.Error.WriteLine($"Error processing file {fileName}: {ex.Message}");
                }
            }

            if (references.Any())
            {
                _context.UserReferencesInMinio.AddRange(references);
                await _context.SaveChangesAsync();
            }

            return Ok($"Successfully processed {references.Count} files to bucket {bucketName}.");
        }



        [HttpPost("TestUploadFile")]
        public async Task<IActionResult> TestUploadFile(IFormFile file)
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
        [HttpPost("TestUploadFileDirectReference")]
        public async Task<IActionResult> TestUploadFileDirectReference(string bucketName, string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File {Path.GetFileName(filePath)} not found.");
            }
            await _minioService.UploadFileAsync(bucketName, Path.GetFileName(filePath), filePath);
            
            return Ok($"File {Path.GetFileName(filePath)} uploaded to bucket {bucketName}.");
        }

        [HttpPost("AddReferenceInPostgre")]
        public async Task<IActionResult> AddReferenceInPostgre(UserReferencesInMinio userReferencesInMinio)
        {
            _context.UserReferencesInMinio.Add(userReferencesInMinio);
            await _context.SaveChangesAsync();

            return CreatedAtAction("AddReferenceInPostgre", new { id = userReferencesInMinio.Id }, userReferencesInMinio);
        }

        [HttpGet("GetFileReferencesByUserId")]
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

        [HttpGet("GetFile")]
        public async Task<IActionResult> GetFile(string bucketName, string objectName)
        {
            var fileUrl = await _minioService.GetFileAsync(bucketName, objectName);
            return Ok(new { Url = fileUrl });
        }

        [HttpGet("GetFileContent")]
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

        // Работа с эксель-файлом
        [HttpGet("GetTableFromExcel")]
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

        // Работа с эксель-файлом
        [HttpGet("GetTableFromExcelWithLimits")]
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


        // Работа с SPC-файлом
        [HttpGet("GetTableFromSPC")]
        public async Task<IActionResult> GetTableFromSPC(string bucketName, string fileName)
        {
            try
            {
                var resultTable = await _minioService.GetSPCFileContentAsJson(bucketName, fileName);
                return Ok(resultTable);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // Работа с несколькими SPC-файлами
        [HttpGet("GetTablesFromSeveralSPC")]
        public async Task<IActionResult> GetTablesFromSeveralSPC(string bucketName, [FromQuery] string[] fileNames) // Вернуть для прода
        //public async Task<IActionResult> GetTablesFromSeveralSPC(string bucketName, [FromQuery] int fileCount = 5) // N файлов для теста
        {
            /*
            // Для теста загрузки файлов
            var stopwatch = Stopwatch.StartNew();
            // Генерация последовательных имен файлов
            var fileNames = new List<string>();
            for (int i = 3; i <= fileCount + 2; i++) // 3.spc → N+2.spc
            {
                fileNames.Add($"allAttachments_6047/SERS4_RamanShift_{i}.spc");
            }
            */

            try
            {
                var results = new List<string>();
                foreach (var fileName in fileNames)
                {
                    var resultTable = await _minioService.GetSPCFileContentAsJson(bucketName, fileName);
                    results.Add(resultTable);
                }

                /*
                // Для теста загрузки файлов
                stopwatch.Stop();
                // Форматирование времени: минуты:секунды (например: 02:45)
                string elapsedFormatted = stopwatch.Elapsed.ToString("mm\\:ss");
                // Память в мегабайтах (1 МБ = 1024 * 1024 байт)
                double memoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                // Логирование для построения медианной кривой (5 измерений)
                string logLine = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {fileNames.Count} files | {elapsedFormatted} | {memoryMb:F2}";
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "SPC_Performance_Log.txt");
                // Асинхронная запись в текстовый файл
                await System.IO.File.AppendAllTextAsync(logFilePath, logLine + Environment.NewLine, Encoding.UTF8);
                */

                // Combine results into a JSON array
                string combinedJson = "[" + string.Join(",", results) + "]";
                GC.Collect();
                return Ok(combinedJson);

                // Для теста загрузки файлов - не выводим json, т.к. Swagger не вывозит
                //return Ok("Successful result");
            }
            catch (Exception ex)
            {
                GC.Collect();
                return NotFound(new { message = ex.Message });
            }
        }

    }
}
