using Microsoft.AspNetCore.Mvc;
using JWTAuthTemplate.Shared.Dtos;
using JWTAuthTemplate.DTO.Identity;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface ISessionService
    {
        Task<int> SaveStatusAsync(Dictionary<string, object> statusParams);

        Task<UserSessionStatusDTO?> GetLatestUserSessionStatusAsync();
        
        Task<IEnumerable<UserSessionStatusDTO>> GetAllStatusesByFileNameAsync(string fileName, string fileExtension);
        
        Task<UserSessionStatusDTO?> GetLatestStatusByFileNameAsync(string fileName, string fileExtension);
        
        Task<UserSessionStatusDTO?> GetLatestStatusByFileNameAndTimeAsync(string fileName, string fileExtension, DateTime asOfTime);
    }
}
