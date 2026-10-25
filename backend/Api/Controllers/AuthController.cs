using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatApp.Application.Interfaces;
using ChatApp.Infrastructure.Services;
using ChatApp.Domain.Entities;
using ChatApp.Api.Configuration;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;
    private readonly JwtSettings _jwtSettings;
    private readonly OtpSettings _otpSettings;

    public AuthController(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOtpService otpService,
        Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings,
        Microsoft.Extensions.Options.IOptions<OtpSettings> otpSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _jwtSettings = jwtSettings.Value;
        _otpSettings = otpSettings.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        var phoneNumber = new PhoneNumber(request.PhoneNumber);
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PhoneNumber.Value == phoneNumber.Value);

        if (user == null)
        {
            return NotFound(new { message = "User not found. Please register first." });
        }

        if (!user.IsPhoneNumberVerified)
        {
            await _otpService.GenerateOtpAsync(phoneNumber.Value, OtpPurpose.Login);
            return Ok(new
            {
                requiresOtp = true,
                otpPurpose = OtpPurpose.Login.ToString(),
                message = "OTP sent to your phone number"
            });
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user.Id, GetClientIpAddress());
        
        user.AddRefreshToken(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            requiresOtp = false,
            accessToken,
            refreshToken = refreshToken.Token,
            user = MapUserToResponse(user)
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        var phoneNumber = new PhoneNumber(request.PhoneNumber);
        
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PhoneNumber.Value == phoneNumber.Value);
        if (existingUser != null)
        {
            return Conflict(new { message = "Phone number already registered" });
        }

        var user = new User(phoneNumber, request.DisplayName);
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _otpService.GenerateOtpAsync(phoneNumber.Value, OtpPurpose.Registration);

        return Ok(new
        {
            requiresOtp = true,
            otpPurpose = OtpPurpose.Registration.ToString(),
            message = "OTP sent to your phone number. Please verify to complete registration."
        });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Phone number and code are required" });
        }

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, true, out var purpose))
        {
            return BadRequest(new { message = "Invalid OTP purpose" });
        }

        var phoneNumber = new PhoneNumber(request.PhoneNumber);
        var isValid = await _otpService.VerifyOtpAsync(phoneNumber.Value, request.Code, purpose);

        if (!isValid)
        {
            return BadRequest(new { message = "Invalid or expired OTP code" });
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PhoneNumber.Value == phoneNumber.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.MarkPhoneNumberAsVerified();
        
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user.Id, GetClientIpAddress());
        
        user.AddRefreshToken(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token,
            user = MapUserToResponse(user)
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var refreshTokenEntity = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.Token == request.RefreshToken && rt.IsActive);

        if (refreshTokenEntity == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        var user = await _unitOfWork.Users.GetByIdAsync(refreshTokenEntity.UserId);
        if (user == null || user.IsDeleted)
        {
            return Unauthorized(new { message = "User not found" });
        }

        refreshTokenEntity.Revoke();
        
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user.Id, GetClientIpAddress());
        
        user.AddRefreshToken(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token,
            user = MapUserToResponse(user)
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        if (request != null && !string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
                rt => rt.Token == request.RefreshToken && rt.UserId == userId);
            
            if (refreshToken != null)
            {
                refreshToken.Revoke();
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(MapUserToResponse(user));
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static object MapUserToResponse(User user)
    {
        return new
        {
            id = user.Id,
            phoneNumber = user.PhoneNumber.Value,
            displayName = user.DisplayName,
            profilePictureUrl = user.ProfilePictureUrl,
            isPhoneNumberVerified = user.IsPhoneNumberVerified,
            status = user.Status.ToString(),
            lastSeenAt = user.LastSeenAt,
            createdAt = user.CreatedAt
        };
    }
}

public record LoginRequest(string PhoneNumber);
public record RegisterRequest(string PhoneNumber, string? DisplayName);
public record VerifyOtpRequest(string PhoneNumber, string Code, string Purpose);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string? RefreshToken);
