using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.WebAPI.Controllers;

[ApiController]
[Route("minio")]
public class MinioController : BaseController
{
    private readonly IMinioService _minioService;

    public MinioController(IMinioService minioService)
    {
        _minioService = minioService;
    }

    [HttpPost("create-bucket")]
    public async Task<IActionResult> CreateBucket(string bucketName)
    {
        return await ExecuteSafeAsync(async () =>
        {
            await _minioService.CreateBucketAsync(bucketName);
            return Ok($"Bucket {bucketName} created");
        });
    }

    [HttpPost("upload-files-update-references")]
    public async Task<IActionResult> UploadFilesUpdateReferences(string bucketName, [FromForm] List<IFormFile> filesData)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _minioService.UploadFilesAsync(bucketName, filesData);

            return Ok($"Successfully processed {result.Count} files to bucket {bucketName}");
        });
    }

    [HttpPost("add-reference")]
    public async Task<IActionResult> AddReference(string userId, string fileName, string fileExtension, string fileReferenceMinio)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var reference = await _minioService.AddReferenceAsync(userId, fileName, fileExtension, fileReferenceMinio);
            return Ok(reference);
        });
    }

    [HttpGet("references")]
    public async Task<IActionResult> GetReferencesByUserId(string userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var references = await _minioService.GetReferencesByUserIdAsync(userId);
            return Ok(references);
        });
    }

    [HttpPost("get-file")]
    public async Task<IActionResult> GetFile(string bucketName, string objectName)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var stream = await _minioService.GetFileAsync(bucketName, objectName);
            return File(stream, "application/octet-stream", objectName);
        });
    }

    [HttpPost("upload-file")]
    public async Task<IActionResult> UploadFile(string bucketName, string objectName, IFormFile file)
    {
        return await ExecuteSafeAsync(async () =>
        {
            using var stream = file.OpenReadStream();
            await _minioService.UploadFileAsync(bucketName, objectName, stream, file.Length);
            return Ok($"File {objectName} uploaded to bucket {bucketName}");
        });
    }

    [HttpPost("get-file-url")]
    public async Task<IActionResult> GetFileUrl(string bucketName, string objectName)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var url = await _minioService.GetFileUrlAsync(bucketName, objectName);
            return Ok(url);
        });
    }

    [HttpPost("get-excel-content")]
    public async Task<IActionResult> GetExcelFileContentAsJson(string bucketName, string objectName)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var content = await _minioService.GetExcelFileContentAsJson(bucketName, objectName);
            return Ok(content);
        });
    }

    [HttpPost("get-excel-content-limits")]
    public async Task<IActionResult> GetExcelFileContentAsJsonWithLimits(string bucketName, string objectName, double inputX1, double inputX2, double inputY1, double inputY2)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var content = await _minioService.GetExcelFileContentAsJsonWithLimits(bucketName, objectName, inputX1, inputX2, inputY1, inputY2);
            return Ok(content);
        });
    }

    [HttpPost("get-spc-content")]
    public async Task<IActionResult> GetSPCFileContentAsJson(string bucketName, string objectName)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var content = await _minioService.GetSPCFileContentAsJson(bucketName, objectName);
            return Ok(content);
        });
    }
}
