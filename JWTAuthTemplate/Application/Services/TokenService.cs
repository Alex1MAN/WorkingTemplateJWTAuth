using JWTAuthTemplate.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JWTAuthTemplate.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public JwtSecurityToken CreateToken(IEnumerable<Claim> claims)
        {
            // Логика создания токена
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            // Логика проверки токена
        }

        public string GenerateRefreshToken()
        {
            // Генерация refresh-токена
        }
    }
}
