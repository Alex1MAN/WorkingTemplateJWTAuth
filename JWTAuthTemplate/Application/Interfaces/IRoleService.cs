using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Models.Identity;

/*
    Administrator — пользователи с полным контролем над системой.
    Moderator — пользователи с ограниченным административным доступом.
    User — обычные зарегистрированные пользователи.
    Guest — незарегистрированные пользователи.
*/

namespace JWTAuthTemplate.Application.Interfaces
{
    public interface IRoleService
    {
        /*
        Task<bool> CreateRole(RoleDTO dto);
        Task<IEnumerable<string>> GetAllRoles();
        */
        
        //Task<(bool Success, string ErrorMessage)> AddRole(RoleDTO dto);

        Task<bool> CreateRoleAsync(string roleName);
        Task<bool> DeleteRoleAsync(string roleName);
        Task<List<string>> GetAllRolesAsync();
        Task<bool> AssignRoleToUserAsync(ApplicationUser user, string roleName);
        Task<bool> RemoveRoleFromUserAsync(ApplicationUser user, string roleName);
        Task<List<string>> GetUserRolesAsync(ApplicationUser user);
        Task<bool> CheckRoleExist(string roleName);
        Task<bool> CheckUserInRoleAsync(ApplicationUser user, string roleName);
    }
}
