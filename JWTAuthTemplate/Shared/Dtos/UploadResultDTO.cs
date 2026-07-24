namespace JWTAuthTemplate.Shared.Dtos;

public class UploadResultDTO
{
    public int Count { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public List<FileReferenceDTO> References { get; set; } = new();
}
