using JWTAuthTemplate.Application.Interfaces;

namespace JWTAuthTemplate.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<ApplicationUser> _userRepository;
        private readonly ITokenService _tokenService;

        public UserService(IRepository<ApplicationUser> userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<bool> Register(RegisterDTO dto)
        {
            // Логика регистрации пользователя

        }

        public async Task<AuthorizedDTO?> Authenticate(LoginDTO dto)
        {
            // Логика аутентификации пользователя

        }
    }
}
