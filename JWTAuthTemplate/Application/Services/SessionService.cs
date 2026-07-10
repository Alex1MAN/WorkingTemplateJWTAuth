using JWTAuthTemplate.Application.Interfaces;
using JWTAuthTemplate.Infrastructure.Database;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using JWTAuthTemplate.Shared.Dtos;
using JWTAuthTemplate.DTO.Identity;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthTemplate.Application.Services
{
    public class SessionService: ISessionService
    {
        private readonly Context _context;

        public SessionService(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        public async Task<int> SaveStatusAsync(string userId, Dictionary<string, object> statusParams)
        {
            // Валидация на уровне Application — можно и в контроллере, но здесь — для полноты
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty or whitespace", nameof(userId));

            if (statusParams == null || !statusParams.Any())
                throw new ArgumentException("Status parameters cannot be empty", nameof(statusParams));

            try
            {
                var record = new UserSessionStatus
                {
                    UserId = userId,
                    ActualAt = DateTime.UtcNow,
                    StatusParamsDict = statusParams
                };

                _context.UserSessionStatuses.Add(record);
                await _context.SaveChangesAsync();

                return record.Id; // assuming UserSessionStatus has Id: Guid
            }
            catch (DbUpdateException dbEx)
            {
                // Логируем dbEx (через ILogger, если внедрён)
                throw new InvalidOperationException("Failed to save session status to database", dbEx);
            }
        }


        public async Task<UserSessionStatusDTO?> GetLatestUserSessionStatusAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty or whitespace", nameof(userId));

            return await _context.UserSessionStatuses
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.ActualAt)
                .Select(u => new UserSessionStatusDTO
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    ActualAt = u.ActualAt,
                    StatusParamsDict = u.StatusParamsDict
                })
                .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<UserSessionStatusDTO>> GetAllStatusesByFileNameAsync(string userId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));
            
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace", nameof(fileName));

            var statuses = await _context.UserSessionStatuses
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.ActualAt)
                .Select(u => new UserSessionStatusDTO
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    ActualAt = u.ActualAt,
                    StatusParamsDict = u.StatusParamsDict // ← десериализуется через NotMapped
                })
                .ToListAsync();

            // Теперь фильтрация в памяти —LINQ to Objects
            return statuses
                .Where(u => u.StatusParamsDict != null &&
                            u.StatusParamsDict.TryGetValue("fileName", out var fileNameValue) &&
                            fileNameValue?.ToString() == fileName);
            
        }


        public async Task<UserSessionStatusDTO?> GetLatestStatusByFileNameAsync(string userId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));
            
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace", nameof(fileName));

            var allStatuses = await _context.UserSessionStatuses
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.ActualAt)
                .Select(u => new UserSessionStatusDTO
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    ActualAt = u.ActualAt,
                    StatusParamsDict = u.StatusParamsDict
                })
                .ToListAsync();

            return allStatuses
                .FirstOrDefault(u => u.StatusParamsDict != null &&
                                     u.StatusParamsDict.TryGetValue("fileName", out var fileNameValue) &&
                                     fileNameValue?.ToString() == fileName);
        }


        public async Task<UserSessionStatusDTO?> GetLatestStatusByFileNameAndTimeAsync(
            string userId, 
            string fileName, 
            DateTime asOfTime)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));
            
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace", nameof(fileName));

            var allStatuses = await _context.UserSessionStatuses
                .Where(u => u.UserId == userId)
                .Where(u => u.ActualAt <= asOfTime) // ← ActualAt — маппинговое поле, можно фильтровать
                .OrderByDescending(u => u.ActualAt)
                .Select(u => new UserSessionStatusDTO
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    ActualAt = u.ActualAt,
                    StatusParamsDict = u.StatusParamsDict
                })
                .ToListAsync();

            return allStatuses
                .FirstOrDefault(u => u.StatusParamsDict != null &&
                                     u.StatusParamsDict.TryGetValue("fileName", out var fileNameValue) &&
                                     fileNameValue?.ToString() == fileName);
        }
    }
}
