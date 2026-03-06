using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Application.Services
{
    public class UserService : IUserService
    {
        //private readonly IRepository<ApplicationUser> _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ITokenService _tokenService;


        //public UserService(IRepository<ApplicationUser> userRepository, ITokenService tokenService)
        public UserService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<(bool Success, string ErrorMessage)> Register([FromBody] RegisterDTO registration)
        {
            // Логика регистрации пользователя
            var userExists = await _userManager.FindByNameAsync(registration.Username);
            var emailExists = await _userManager.FindByEmailAsync(registration.Email);
            if (userExists != null)
            {
                return (false, "That username already exists!");
            }
            if (emailExists != null)
            {
                return (false, "That email is already in use!");
            }
            string bucketName = "";
            var user = new ApplicationUser()
            {
                Id = Guid.NewGuid().ToString(),
                Email = registration.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registration.Username,
                CreateDate = DateTime.UtcNow,
            };

            // Добавление пользователя в базу данных
            var result = await _userManager.CreateAsync(user, registration.Password);
            if (!result.Succeeded)
            {
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Присвоение начальной роли
            // await _userManager.AddToRoleAsync(user, "User");

            return (true, "");

            /*
            // Minio позже
            bucketName = user.Id;
            try
            {
                var result = await _userManager.CreateAsync(user, registration.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }
                // Создаем бакет в minio
                await _minioService.CreateBucketAsync(bucketName);
                return Ok(new { success = true, message = "User (and bucket in Minio) created successfully!" });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            */
        }

        /*
        public async Task<AuthorizedDTO?> Authenticate(LoginDTO dto)
        {
            // Логика аутентификации пользователя

        }
        */
    }
}
