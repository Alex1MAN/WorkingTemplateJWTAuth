using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace JWTAuthTemplate.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;


        /*private readonly IRoleService<ApplicationRole> _roleRepository;

        public RoleService(IRoleService<ApplicationRole> roleRepository)
        {
            _roleRepository = roleRepository;
        }
        
        public async Task<bool> CreateRole(RoleDTO dto)
        {
            // Логика создания роли
        }

        public async Task<IEnumerable<string>> GetAllRoles()
        {
            // Получение списка ролей
        }
        */


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
    }
}
