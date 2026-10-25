using System;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string? ipAddress = null);
    bool ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
    DateTime GetTokenExpiration(string token);
}