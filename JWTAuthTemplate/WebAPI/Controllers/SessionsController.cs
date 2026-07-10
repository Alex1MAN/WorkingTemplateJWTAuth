using Microsoft.AspNetCore.Mvc;
using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.Shared.Dtos; // ← предположительно будет DTO-класс, например, UserSessionStatusDto


namespace JWTAuthTemplate.WebAPI.Controllers
{
    [ApiController]
    [Route("sessions")]
    public class SessionsController : BaseController
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }


        [HttpPost("save-status")]
        public async Task<IActionResult> SaveStatus(string userId, [FromBody] Dictionary<string, object> statusParams)
        {
            if (statusParams == null)
                return BadRequest("Request body cannot be empty");

            try
            {
                var id = await _sessionService.SaveStatusAsync(userId, statusParams);
                return Ok(new { id, message = "Status saved successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException) // от SessionService
            {
                return Problem(statusCode: 500, detail: "Failed to save session status");
            }
        }


        [HttpGet("latest-status/{userId}")]
        public async Task<IActionResult> GetLatestStatus(string userId)
        {
            try
            {
                var status = await _sessionService.GetLatestUserSessionStatusAsync(userId);
                return status is not null ? Ok(status) : NotFound($"No session status found for user {userId}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("all-by-file-name")]
        public async Task<IActionResult> GetAllStatusesByFileName([FromQuery] string userId, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("Parameter 'userId' is required");
            
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest("Parameter 'fileName' is required");

            try
            {
                var statuses = await _sessionService.GetAllStatusesByFileNameAsync(userId, fileName);

                if (!statuses.Any()) return NotFound($"No session statuses found for user '{userId}' and file '{fileName}'");

                return Ok(statuses);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("latest-by-file-name")]
        public async Task<IActionResult> GetLatestStatusByFileName([FromQuery] string userId, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("Parameter 'userId' is required");
            
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Parameter 'fileName' is required");

            try
            {
                var status = await _sessionService.GetLatestStatusByFileNameAsync(userId, fileName);
                return status is not null ? Ok(status) : NotFound($"No session status found for user '{userId}' and file '{fileName}'");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("latest-by-file-name-and-time")]
        public async Task<IActionResult> GetLatestStatusByFileNameAndTime(
            [FromQuery] string userId,
            [FromQuery] string fileName,
            [FromQuery] string asOfTime)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("Parameter 'userId' is required");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest("Parameter 'fileName' is required");
            if (asOfTime is null) return BadRequest("Parameter 'asOfTime' is required");

            if (!DateTime.TryParse(asOfTime, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var asOfTimeUtc)) return BadRequest("Parameter 'asOfTime' must be a valid ISO 8601 datetime string (e.g., 2026-04-03T17:05:00Z or 2026-04-03T17:05:00)");

            try
            {
                var status = await _sessionService.GetLatestStatusByFileNameAndTimeAsync(userId, fileName, asOfTimeUtc);
                return status is not null ? Ok(status) : NotFound($"No session status found for user '{userId}', file '{fileName}' as of {asOfTime}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
