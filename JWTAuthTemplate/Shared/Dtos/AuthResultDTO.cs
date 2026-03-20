using JWTAuthTemplate.DTO.Identity;

namespace JWTAuthTemplate.Shared.Dtos
{
    public class AuthResultDTO
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public UserDTO User { get; set; }
    }
}
