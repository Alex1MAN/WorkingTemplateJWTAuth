using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace JWTAuthTemplate.WebAPI.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public AuthenticationController(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            return await ExecuteSafeAsync(() => _userService.Register(dto));
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            return await ExecuteSafeAsync(async () =>
            {
                var authResult = await _userService.Authenticate(dto);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResult.User.Id),
                    new Claim(ClaimTypes.Name, authResult.User.Username),
                };
                var claimsIdentity = new ClaimsIdentity(claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true });
                Response.Cookies.Append("refreshToken", authResult.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    });
            });
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            return await ExecuteSafeAsync(async () => await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme));
        }


    }
}
