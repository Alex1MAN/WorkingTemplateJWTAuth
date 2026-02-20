using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using Microsoft.AspNetCore.Mvc;

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
            var authorizedData = await _userService.Authenticate(dto);
            return authorizedData != null ? Ok(authorizedData) : Unauthorized();
        }

        [HttpPost("add-role")]
        public async Task<IActionResult> AddRole([FromBody] RoleDTO dto)
        {
            return await ExecuteSafeAsync(() => _roleService.CreateRole(dto));
        }

        [HttpGet("get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleService.GetAllRoles();
            return Ok(roles);
        }
    }
}
