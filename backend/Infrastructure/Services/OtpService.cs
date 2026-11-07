using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Api.Configuration;

namespace ChatApp.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OtpSettings _settings;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IUnitOfWork unitOfWork, IOptions<OtpSettings> settings, ILogger<OtpService> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose)
    {
        var expiredOtps = await _unitOfWork.OtpCodes.FindAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && o.ExpiresAt <= DateTime.UtcNow);
        
        foreach (var expiredOtp in expiredOtps)
        {
            _unitOfWork.OtpCodes.Remove(expiredOtp);
        }

        var recentOtp = await _unitOfWork.OtpCodes.FirstOrDefaultAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && o.ExpiresAt > DateTime.UtcNow && o.VerifiedAt == null);

        if (recentOtp != null)
        {
            return recentOtp.Code;
        }

        var code = GenerateCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);
        
        var newOtp = new OtpCode(phoneNumber, code, purpose, expiresAt);
        newOtp.MaxAttempts = _settings.MaxAttempts;
        
        await _unitOfWork.OtpCodes.AddAsync(newOtp);
        await _unitOfWork.SaveChangesAsync();

        // Development provider: no SMS is sent, so print the code for the developer.
        // Never write OTPs to the console or logs with any other provider.
        if (string.Equals(_settings.Provider, "Development", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[DEV] OTP for {phoneNumber} ({purpose}): {code}");
        }

        return code;
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string code, OtpPurpose purpose)
    {
        var candidates = await _unitOfWork.OtpCodes.FindAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && o.ExpiresAt > DateTime.UtcNow && o.VerifiedAt == null);

        if (candidates.Count == 0)
        {
            _logger.LogWarning(
                "OTP verification failed for {Phone} ({Purpose}): no active code (never sent, expired, or already used)",
                phoneNumber, purpose);
            return false;
        }

        // Concurrent requests can leave more than one active code; accept a match against any of them.
        var otp = candidates.FirstOrDefault(o => o.CanAttempt && o.Code == code)
                  ?? candidates.OrderByDescending(o => o.CreatedAt).First();

        var isValid = otp.Verify(code);

        if (isValid)
        {
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        _logger.LogWarning(
            "OTP verification failed for {Phone} ({Purpose}): wrong code or attempts exhausted ({Attempts}/{Max})",
            phoneNumber, purpose, otp.AttemptCount, otp.MaxAttempts);

        if (otp.IsMaxAttemptsReached)
        {
            _unitOfWork.OtpCodes.Remove(otp);
        }

        await _unitOfWork.SaveChangesAsync();
        return false;
    }

    public async Task<bool> IsOtpValidAsync(string phoneNumber, OtpPurpose purpose)
    {
        var otp = await _unitOfWork.OtpCodes.FirstOrDefaultAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && o.ExpiresAt > DateTime.UtcNow && o.VerifiedAt == null);

        return otp != null;
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        var expiredOtps = await _unitOfWork.OtpCodes.FindAsync(o => o.ExpiresAt <= DateTime.UtcNow);
        foreach (var expiredOtp in expiredOtps)
        {
            _unitOfWork.OtpCodes.Remove(expiredOtp);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private string GenerateCode()
    {
        var min = (int)Math.Pow(10, _settings.Length - 1);
        var max = (int)Math.Pow(10, _settings.Length);
        // Cryptographically secure; System.Random is predictable. Upper bound is exclusive.
        return RandomNumberGenerator.GetInt32(min, max).ToString();
    }
}
