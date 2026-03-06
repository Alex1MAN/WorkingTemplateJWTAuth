using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
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

        /*
        [HttpGet("{username}")]
        public async Task<IActionResult> Get(string username)
        {
            return await ExecuteSafeAsync(async () =>
            {
                await _userService.GetByUsername(username);
            });
        }
        */
    }
}
