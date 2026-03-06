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
        private readonly IMinioService _minioService;


        //public UserService(IRepository<ApplicationUser> userRepository, ITokenService tokenService)
        public UserService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IMinioService minioService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _minioService = minioService;
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

            // Присвоение начальной роли - проверить также через if (!result.Succeeded)
            // var result = await _userManager.AddToRoleAsync(user, "User");

            // Создаем бакет в minio
            string bucketName = user.Id;
            try
            {
                await _minioService.CreateBucketAsync(bucketName);
                return (true, "User (and bucket in Minio) created successfully!");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        /*
        public async Task<AuthorizedDTO?> Authenticate(LoginDTO dto)
        {
            // Логика аутентификации пользователя

        }
        */
    }
}
