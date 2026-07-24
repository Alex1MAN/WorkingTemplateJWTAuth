using JWTAuthTemplate.Shared.Dtos;

namespace JWTAuthTemplate.Application.Interfaces;

public interface IMinioService
{
    Task CreateBucketAsync(string bucketName);
    Task<string> GetObjectETagAsync(string bucketName, string objectName);
    Task<Stream> GetFileAsync(string bucketName, string objectName);
    Task UploadFileAsync(string bucketName, string fileName, Stream dataStream, long length);
    Task<UploadResultDTO> UploadFilesAsync(string bucketName, List<IFormFile> filesData);
    Task<FileReferenceDTO> AddReferenceAsync(string userId, string fileName, string fileExtension, string fileReferenceMinio);
    Task<List<FileReferenceDTO>> GetReferencesByUserIdAsync(string userId);
    Task<string> GetFileUrlAsync(string bucketName, string objectName);
    Task<string> GetExcelFileContentAsJson(string bucketName, string objectName);
    Task<string> GetExcelFileContentAsJsonWithLimits(string bucketName, string objectName, double inputX1, double inputX2, double inputY1, double inputY2);
    Task<string> GetSPCFileContentAsJson(string bucketName, string objectName);
}
