using Microsoft.AspNetCore.Mvc;
using JWTAuthTemplate.Shared.Dtos;
using JWTAuthTemplate.DTO.Identity;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface ISessionService
    {
        // Возвращает ID созданной сущности или null при ошибке (но лучше — исключение)
        Task<int> SaveStatusAsync(string userId, Dictionary<string, object> statusParams);

        Task<UserSessionStatusDTO?> GetLatestUserSessionStatusAsync(string userId);
        
        Task<IEnumerable<UserSessionStatusDTO>> GetAllStatusesByFileNameAsync(string userId, string fileName);
        
        Task<UserSessionStatusDTO?> GetLatestStatusByFileNameAsync(string userId, string fileName);
        
        Task<UserSessionStatusDTO?> GetLatestStatusByFileNameAndTimeAsync(string userId, string fileName, DateTime asOfTime);
    }
}
