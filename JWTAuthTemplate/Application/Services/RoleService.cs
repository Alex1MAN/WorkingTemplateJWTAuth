using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JWTAuthTemplate.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<bool> CreateRoleAsync(string roleName)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var result = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                return result.Succeeded;
            }
            return false;
        }

        public async Task<bool> DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                return result.Succeeded;
            }
            return false;
        }

        public async Task<List<string>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        }

        public async Task<bool> AssignRoleToUserAsync(ApplicationUser user, string roleName)
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<bool> RemoveRoleFromUserAsync(ApplicationUser user, string roleName)
        {
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<List<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return (List<string>)await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> CheckRoleExist(string roleName)
        {
            return await _roleManager.RoleExistsAsync(roleName);
        }

        public async Task<bool> CheckUserInRoleAsync(ApplicationUser user, string roleName)
        {
            return await _userManager.IsInRoleAsync(user, roleName);
        }

        /*
        public async Task<(bool Success, string ErrorMessage)> AddRole([FromBody] RoleDTO role)
        {
            var roleExists = await _roleManager.FindByNameAsync(role.Name);
            if (roleExists != null)
            {
                return (false, "That role already exists!");
            }
            var newRole = new ApplicationRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = role.Name,
                NormalizedName = role.Name.ToUpper(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            };
            try
            {
                await _roleManager.CreateAsync(newRole);
                return (true, "Role created successfully!");

            }
            catch (Exception e)
            {
                throw new ValidationException(e.Message);
            }
        }
        */
    }
}
