using JWTAuthTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JWTAuthTemplate.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public JwtSecurityToken CreateToken(IEnumerable<Claim> claims)
        {
            // Логика создания токена
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
            _ = int.TryParse(_configuration["JWT:TokenValidityInDays"], out int tokenValidityInDays);
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddDays(tokenValidityInDays),
                claims: claims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
            return token;
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            // Логика проверки токена
            var options = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);
            var tokenValidationParameters = options.TokenValidationParameters;
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }

        public string GenerateRefreshToken()
        {
            // Генерация refresh-токена
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
