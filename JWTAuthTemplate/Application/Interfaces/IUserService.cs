using JWTAuthTemplate.DTO.Identity;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface IUserService
    {
        Task<(bool Success, string ErrorMessage)> Register(RegisterDTO dto);
        //Task<AuthorizedDTO?> Authenticate(LoginDTO dto);
    }
}
