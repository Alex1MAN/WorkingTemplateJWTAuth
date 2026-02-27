using Microsoft.AspNetCore.Mvc;
using JWTAuthTemplate.Application.Interfaces;

namespace JWTAuthTemplate.WebAPI.Controllers
{
    [ApiController]
    [Route("sessions")]

    public class SessionsController : BaseController
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost("save-status/{userid}")]
        public async Task<IActionResult> SaveStatus(string userid, [FromBody] Dictionary<string, object> statusParams)
        {
            return await ExecuteSafeAsync(async () =>
            {
                await _sessionService.SaveUserSessionStatus(userid, statusParams);
            });
        }

        [HttpGet("latest-status/{userid}")]
        public async Task<IActionResult> GetLatestStatus(string userid)
        {
            return await ExecuteSafeAsync(async () =>
            {
                await _sessionService.GetLatestUserSessionStatus(userid);
            });
        }
    }
}
