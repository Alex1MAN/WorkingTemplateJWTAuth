﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JWTAuthTemplate.Context;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Extensions;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Minio;

namespace JWTAuthTemplate.Controllers
{
    [ApiController]
    [Route("Auth")]
    public class AuthenticationController: ControllerBase
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;

        private readonly MinioService _minioService;

        public AuthenticationController(UserManager<ApplicationUser> signInManager, IConfiguration configuration, RoleManager<ApplicationRole> roleManager, IOptionsMonitor<JwtBearerOptions> jwtOptions, MinioService minioService)
        {
            _userManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions;
            _minioService = minioService;
        }

        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromBody] RegisterDTO registration)
        {
            var userExists = await _userManager.FindByNameAsync(registration.Username);
            var emailExists = await _userManager.FindByEmailAsync(registration.Email);

            if (userExists != null)
            {
                return BadRequest("That username already exists!");
            }

            if (emailExists != null)
            {
                return BadRequest("That email is already in use!");
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
            bucketName = user.Id;
            Console.WriteLine("bucketName:" + bucketName);

            try
            {
                var result = await _userManager.CreateAsync(user, registration.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }
                //return Ok("User created successfully!");
                
                // Создаем бакет в minio
                await _minioService.CreateBucketAsync(bucketName);

                return Ok(new { success = true, message = "User (and bucket in Minio) created successfully!" });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginDTO login)
        {
            var user = await _userManager.FindByNameAsync(login.Username);
            if (user == null)
            {
                return BadRequest("Invalid username or password!");
            }

            var result = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!result)
            {
                return BadRequest("Invalid username or password!");
            }
            
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); //03.01.2025

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = true }); //03.01.2025

            var token = CreateToken(claims.ToList()); //10.01.2025
            var refreshToken = GenerateRefreshToken(); //10.01.2025

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30) // Установите время жизни cookie
            });
            // Обновление информации о пользователе с новым refresh токеном (если требуется)
            user.RefreshToken = refreshToken;

            await _userManager.UpdateAsync(user);

            //return Ok(); // Return appropriate response, 03.01.2025; 10.01.2025

            return Ok(new
            {
                JWT = new AuthorizedDTO()
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    /*RefreshToken = refreshToken,*/
                    //TokenExpiration = token.ValidTo,
                    //TokenExpiration = DateTime.UtcNow.AddDays(30)
                },
                User = new UserDTO()
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    CreateDate = user.CreateDate,
                }
            });
        }

        [HttpPost("Logout")] // 03.01.2025 - all method
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpPost("Roles")]
        public async Task<ActionResult> AddRole([FromBody] RoleDTO role)
        {
            var roleExists = await _roleManager.FindByNameAsync(role.Name);
            if (roleExists != null)
            {
                return BadRequest("That role already exists!");
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
                return Ok("Role created successfully!");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Refresh")]
        public async Task<ActionResult> Refresh([FromBody] RefreshDTO model)
        {

            string? accessToken = model.AccessToken;
            string? refreshToken = model.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return BadRequest("Invalid access token or refresh token");
            }

            string? name = principal.Identity?.Name;
            if (name == null)
            {
                return BadRequest("Invalid access token or refresh token");
            }
            string username = name;


            var user = await _userManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid access token or refresh token");
            }
            var userRoles = await _userManager.GetRolesAsync(user);

            var newAccessToken = CreateToken(principal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                JWT = new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                    RefreshToken = newRefreshToken,
                    Expiration = newAccessToken.ValidTo
                },
                User = new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    roles = userRoles
                }

            });
        }

        [Authorize] // 03.01.2025 - decorate protected API endpoints with the [Authorize] attribute to ensure only authenticated users can access them
        [HttpGet("Roles")]
        public async Task<ActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles);
        }


        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
            _ = int.TryParse(_configuration["JWT:TokenValidityInDays"], out int tokenValidityInDays);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddDays(tokenValidityInDays),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            //get token validation configuration from addjwtbearer
            var options = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);
            var tokenValidationParameters = options.TokenValidationParameters;

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;

        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

    }
}
