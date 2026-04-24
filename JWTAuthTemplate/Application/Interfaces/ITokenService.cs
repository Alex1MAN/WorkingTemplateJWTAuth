using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateRefreshToken();

        Task<(JwtSecurityToken AccessToken, string RefreshToken)> RefreshTokensAsync(string accessToken, string refreshToken);

        ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token);

        JwtSecurityToken CreateToken(List<Claim> authClaims);
    }
}
