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

        
        [HttpGet("get-by-username")]
        public async Task<IActionResult> Get(string username)
        {
            var userDto = await _userService.GetByUsername(username);
            if (userDto is null)
            {
                return NotFound($"User with username '{username}' was not found");
            }
            return Ok(userDto);
        }
    }
}
