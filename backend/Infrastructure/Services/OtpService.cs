using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Api.Configuration;

namespace ChatApp.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OtpSettings _settings;
    private readonly Random _random = new();

    public OtpService(IUnitOfWork unitOfWork, IOptions<OtpSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose)
    {
        var expiredOtps = await _unitOfWork.OtpCodes.FindAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && o.IsExpired);
        
        foreach (var expiredOtp in expiredOtps)
        {
            _unitOfWork.OtpCodes.Remove(expiredOtp);
        }

        var recentOtp = await _unitOfWork.OtpCodes.FirstOrDefaultAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && !o.IsExpired && !o.IsVerified);

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

        Console.WriteLine($"[DEV] OTP for {phoneNumber} ({purpose}): {code}");

        return code;
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string code, OtpPurpose purpose)
    {
        var otp = await _unitOfWork.OtpCodes.FirstOrDefaultAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && !o.IsExpired && !o.IsVerified);

        if (otp == null)
            return false;

        var isValid = otp.Verify(code);
        
        if (isValid)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        else if (otp.IsMaxAttemptsReached)
        {
            _unitOfWork.OtpCodes.Remove(otp);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return isValid;
    }

    public async Task<bool> IsOtpValidAsync(string phoneNumber, OtpPurpose purpose)
    {
        var otp = await _unitOfWork.OtpCodes.FirstOrDefaultAsync(
            o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && !o.IsExpired && !o.IsVerified);

        return otp != null;
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        var expiredOtps = await _unitOfWork.OtpCodes.FindAsync(o => o.IsExpired);
        foreach (var expiredOtp in expiredOtps)
        {
            _unitOfWork.OtpCodes.Remove(expiredOtp);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private string GenerateCode()
    {
        var min = (int)Math.Pow(10, _settings.Length - 1);
        var max = (int)Math.Pow(10, _settings.Length) - 1;
        return _random.Next(min, max).ToString();
    }
}
