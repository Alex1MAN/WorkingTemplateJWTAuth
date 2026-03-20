using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface IUserService
    {
        Task<(bool Success, string ErrorMessage)> Register(RegisterDTO dto);
        Task<AuthResultDTO> Authenticate(LoginDTO dto);
    }
}
