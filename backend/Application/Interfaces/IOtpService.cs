using System;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose);
    Task<bool> VerifyOtpAsync(string phoneNumber, string code, OtpPurpose purpose);
    Task<bool> IsOtpValidAsync(string phoneNumber, OtpPurpose purpose);
    Task CleanupExpiredOtpsAsync();
}