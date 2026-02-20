namespace JWTAuthTemplate.Application.Interfaces
{
    public interface IMinioUploader
    {
        Task UploadFileAsync(string bucketName, string fileName, Stream dataStream, long length);
        Task<string> GetFileReferenceAsync(string bucketName, string fileName);

    }
}
