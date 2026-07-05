namespace JWTAuthTemplate.Models.Identity
{
    public class CompleteUploadRequest
    {   public string UploadId { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string BucketName { get; set; } = null!;
        public int TotalChunks { get; set; }
    }
}
