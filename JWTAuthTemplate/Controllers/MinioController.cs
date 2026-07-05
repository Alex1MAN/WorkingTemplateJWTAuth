using JWTAuthTemplate.Context;
using JWTAuthTemplate.Extensions;
using JWTAuthTemplate.Migrations;
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
        private readonly string _tempRoot;  // Убрать в develop работу с временными файлами

        public MinioController(MinioService minioService, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _minioService = minioService;
            _context = context;
            _tempRoot = Path.Combine(env.ContentRootPath, "UploadsTemp");
            if (!Directory.Exists(_tempRoot))
                Directory.CreateDirectory(_tempRoot);  // Убрать в develop работу с временными файлами
        }


        // Убрать в develop работу с временными файлами
        [HttpPost("UploadChunk")]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> UploadChunk([FromForm] string bucketName,
                                                     [FromForm] string uploadId,
                                                     [FromForm] string fileName,
                                                     [FromForm] int chunkIndex,
                                                     [FromForm] int totalChunks,
                                                     [FromForm] IFormFile? chunk)
        {
            if (chunk == null) return BadRequest("Chunk missing");

            // Папка для текущей сессии загрузки
            var sessionDir = Path.Combine(_tempRoot, SanitizePath(bucketName), SanitizePath(uploadId));
            Directory.CreateDirectory(sessionDir);

            // Сохраняем чанк под именем индексного файла, чтобы потом объединить в порядке
            var chunkPath = Path.Combine(sessionDir, $"{chunkIndex}.part");
            using (var fs = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await chunk.CopyToAsync(fs);
            }

            // Можно вернуть простой статус; клиент продолжит отправку следующих чанков
            return Ok(new { success = true, chunkIndex });
        }

        
        // Убрать в develop работу с временными файлами
        [HttpPost("CompleteUpload")]
        public async Task<IActionResult> CompleteUpload([FromBody] CompleteUploadRequest req)
        {
            if (req == null) return BadRequest("Invalid request");
            if (string.IsNullOrWhiteSpace(req.BucketName)) return BadRequest("BucketName is required");
            if (string.IsNullOrWhiteSpace(req.FileName)) return BadRequest("FileName is required");
            if (string.IsNullOrWhiteSpace(req.UploadId)) return BadRequest("UploadId is required");
            if (req.TotalChunks <= 0) return BadRequest("TotalChunks must be > 0");

            var sessionDir = Path.Combine(_tempRoot,
                SanitizePath(req.BucketName),
                SanitizePath(req.UploadId));

            if (!Directory.Exists(sessionDir))
                return NotFound("Upload session not found");

            var finalDir = Path.Combine(_tempRoot, "Completed", SanitizePath(req.BucketName));
            Directory.CreateDirectory(finalDir);

            var tempFinalPath = Path.Combine(finalDir, SanitizePath(req.FileName));

            // 1) Собираем чанки в один временный файл на диске
            using (var output = new FileStream(tempFinalPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                for (int i = 0; i < req.TotalChunks; i++)
                {
                    var partPath = Path.Combine(sessionDir, $"{i}.part");
                    if (!System.IO.File.Exists(partPath))
                        return BadRequest($"Missing chunk {i}");

                    using (var partStream = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await partStream.CopyToAsync(output);
                    }
                }
            }

            string? etag = null;
            try
            {
                // 2) Загружаем итоговый файл в MinIO (как раньше в UploadFilesUpdateReferences)
                await _minioService.UploadFileAsync(req.BucketName, req.FileName, tempFinalPath);

                // 3) Получаем ссылку/ETag как раньше
                etag = await _minioService.GetObjectETagAsync(req.BucketName, req.FileName);

                // 4) Записываем reference в БД (как раньше)
                var reference = new UserReferencesInMinio
                {
                    UserId = req.BucketName,
                    FileName = req.FileName,
                    FileExtension = Path.GetExtension(req.FileName).Replace(".", ""),
                    FileReferenceMinio = etag
                };

                _context.UserReferencesInMinio.Add(reference);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, fileName = req.FileName, fileReferenceMinio = etag });
            }
            finally
            {
                // 5) Чистим temp-чашки/директории (как раньше clean tempFilePath в foreach)
                try { if (Directory.Exists(sessionDir)) Directory.Delete(sessionDir, true); } catch { }
                try { if (System.IO.File.Exists(tempFinalPath)) System.IO.File.Delete(tempFinalPath); } catch { }
            }
        }

        
        // Убрать в develop работу с временными файлами
        private static string SanitizePath(string input)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input;
        }


        [HttpPost("CreateBucket")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            await _minioService.CreateBucketAsync(bucketName);
            return Ok($"Bucket {bucketName} created.");
        }

        
        // Основной метод загрузки файлов
        [HttpPost("UploadFilesUpdateReferences")]
        public async Task<IActionResult> UploadFilesUpdateReferences(
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
                        //fileUrl = await reader.ReadToEndAsync(); // Проблема тут, т.к. "в fileUrl пишется весь поток данных, а не только поле ETAG которое нам нужно"

                        // 13.12.2025 корректировка
                        fileUrl = await _minioService.GetObjectETagAsync(bucketName, fileName);
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
        [HttpPost("UploadFilesUpdateReferences_2")]
        public async Task<IActionResult> UploadFilesUpdateReferences_2(
            [FromForm] string bucketName,
            [FromForm] List<IFormFile> filesData)
        {
            // Вроде рабочее, но нужно проверить
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
                        //fileUrl = await reader.ReadToEndAsync(); // Проблема тут, т.к. "в fileUrl пишется весь поток данных, а не только поле ETAG которое нам нужно"

                        // 13.12.2025 корректировка
                        fileUrl = await _minioService.GetObjectETagAsync(bucketName, fileName);
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


        [HttpGet("GetUrlFileFromMinio")]
        public async Task<IActionResult> GetUrlFileFromMinio(string bucketName, string objectName)
        {
            var fileUrl = await _minioService.GetFileAsync(bucketName, objectName);
            return Ok(new { Url = fileUrl });
        }

        /*
        // 02.01.2026 - мб устарело
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
        */


        // Работа с эксель-файлом
        [HttpGet("GetAllTableFromExcel")]
        public async Task<IActionResult> GetAllTableFromExcel(string bucketName, string fileName)
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


        // Работа с эксель-файлом с ограничениями
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


        // Работа с несколькими SPC-файлами - работу с одним файлом убрать
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


        // ======================= Новые методы добавлять ниже

        [HttpGet("GetAllContentFromFiles")]
        public async Task<IActionResult> GetAllContentFromFiles(string bucketName, [FromQuery] string[] fileNames, string allFilesExtension, double inputX1, double inputX2, double inputY1, double inputY2)
        {
            allFilesExtension = allFilesExtension.ToLower();
            switch (allFilesExtension)
            {
                case var s when s.Contains("xls"):
                    try
                    {
                        // Если все 4 границы равны, получаем весь контент, иначе - по границам
                        if (inputX1 == inputX2 && inputX2 == inputY1 && inputY1 == inputY2)
                        {
                            // Работа только с ПЕРВЫМ эксель файлом (если требуется работа с несколькими файлами - доработать метод)
                            var resultTable = await _minioService.GetExcelFileContentAsJson(bucketName, fileNames[0]);
                            GC.Collect();
                            return Ok(resultTable);
                        }
                        else
                        {
                            try
                            {
                                var resultTable = await _minioService.GetExcelFileContentAsJsonWithLimits(bucketName, fileNames[0], inputX1, inputX2, inputY1, inputY2);
                                return Ok(resultTable);
                            }
                            catch (Exception ex)
                            {
                                return NotFound(new { message = ex.Message });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GC.Collect();
                        return NotFound(new { message = ex.Message });
                    }
                
                case var s when s.Contains("spc"):
                    try
                    {
                        var results = new List<string>();
                        foreach (var fileName in fileNames)
                        {
                            var resultTable = await _minioService.GetSPCFileContentAsJson(bucketName, fileName);
                            results.Add(resultTable);
                        }
                        // Combine results into a JSON array
                        string combinedJson = "[" + string.Join(",", results) + "]";
                        GC.Collect();
                        return Ok(combinedJson);
                    }
                    catch (Exception ex)
                    {
                        GC.Collect();
                        return NotFound(new { message = ex.Message });
                    }
                
                default:
                    GC.Collect();
                    return BadRequest(new { allFilesExtension, error = "Неизвестное расширение файлов" });
            }
        }
    }
}
