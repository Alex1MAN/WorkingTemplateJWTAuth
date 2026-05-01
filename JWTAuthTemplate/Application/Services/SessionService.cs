using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.Infrastructure.Database;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Application.Services
{
    public class SessionService: ISessionService
    {
        private readonly Context _context;

        public SessionService(Context context)
        {
            _context = context;
        }


        public async Task<(bool Success, string ErrorMessage)> SaveStatus(string userId, [FromBody] Dictionary<string, object> statusParams)
        {
            try
            {
                var record = new UserSessionStatus
                {
                    UserId = userId,
                    ActualAt = DateTime.UtcNow,
                    StatusParamsDict = statusParams
                };
                _context.UserSessionStatuses.Add(record);
                await _context.SaveChangesAsync();
                return (true, "Current status successfully saved");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }
    }
}
