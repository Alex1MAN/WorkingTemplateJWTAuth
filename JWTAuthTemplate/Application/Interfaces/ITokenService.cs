using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface ITokenService
    {
        JwtSecurityToken CreateToken(IEnumerable<Claim> claims);
        ClaimsPrincipal ValidateToken(string token);
        string GenerateRefreshToken();
    }
}
