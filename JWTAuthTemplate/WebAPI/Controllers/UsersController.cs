using JWTAuthTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.WebAPI.Controllers
{
    [ApiController]
    [Route("users")]

    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> Get(string username)
        {
            return await ExecuteSafeAsync(async () =>
            {
                var user = await _userService.GetByUsername(username);
                return Ok(user);
            });
        }
    }
}
