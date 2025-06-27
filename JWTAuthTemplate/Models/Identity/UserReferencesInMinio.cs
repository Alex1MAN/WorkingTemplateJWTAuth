namespace JWTAuthTemplate.Models.Identity
{
    public class UserReferencesInMinio
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileReferenceMinio { get; set; }
    }
}
