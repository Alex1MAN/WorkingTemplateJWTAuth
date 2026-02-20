using JWTAuthTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.WebAPI.Controllers
{
    [ApiController]
    [Route("roles")]

    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPut("{username}/{rolename}")]
        public async Task<IActionResult> AssignRole(string username, string rolename)
        {
            return await ExecuteSafeAsync(async () =>
            {
                await _roleService.AssignRoleToUser(username, rolename);
                return Ok($"Role {rolename} assigned to {username}");
            });
        }

        [HttpDelete("{username}/{rolename}")]
        public async Task<IActionResult> RevokeRole(string username, string rolename)
        {
            return await ExecuteSafeAsync(async () =>
            {
                await _roleService.RevokeRoleFromUser(username, rolename);
                return Ok($"Role {rolename} revoked from {username}");
            });
        }
    }
}
