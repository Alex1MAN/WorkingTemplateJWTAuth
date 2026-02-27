using JWTAuthTemplate.DTO.Identity;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface IUserService
    {
        Task Register(RegisterDTO dto);
        Task<AuthorizedDTO?> Authenticate(LoginDTO dto);
    }
}
