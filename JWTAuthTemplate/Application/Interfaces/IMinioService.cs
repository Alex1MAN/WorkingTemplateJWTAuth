namespace JWTAuthTemplate.Application.Interfaces
{
    public interface IMinioService
    {
        /*Task UploadFileAsync(string bucketName, string fileName, Stream dataStream, long length);
        Task<string> GetFileReferenceAsync(string bucketName, string fileName);*/
        Task CreateBucketAsync(string bucketName);
    }
}
