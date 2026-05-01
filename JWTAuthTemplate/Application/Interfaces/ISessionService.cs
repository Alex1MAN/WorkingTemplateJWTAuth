using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface ISessionService
    {
        Task<(bool Success, string ErrorMessage)> SaveStatus(string userid, [FromBody] Dictionary<string, object> statusParams);
        
        //Task GetLatestUserSessionStatus(string userid);
    }
}
