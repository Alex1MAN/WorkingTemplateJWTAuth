using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface ISessionService
    {
        Task SaveUserSessionStatus(string userid, [FromBody] Dictionary<string, object> statusParams);
        Task GetLatestUserSessionStatus(string userid);
    }
}
