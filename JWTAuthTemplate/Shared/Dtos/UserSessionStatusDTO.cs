namespace JWTAuthTemplate.DTO.Identity
{
    public class UserSessionStatusDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime ActualAt { get; set; }
        public Dictionary<string, object>? StatusParamsDict { get; set; }
    }
}
