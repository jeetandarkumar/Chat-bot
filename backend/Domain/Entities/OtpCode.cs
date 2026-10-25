using System;

namespace ChatApp.Domain.Entities;

public class OtpCode
{
    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public OtpPurpose Purpose { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; set; } = 3;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsVerified => VerifiedAt != null;
    public bool IsMaxAttemptsReached => AttemptCount >= MaxAttempts;
    public bool CanAttempt => !IsExpired && !IsVerified && !IsMaxAttemptsReached;

    private OtpCode() { } // EF Core

    public OtpCode(string phoneNumber, string code, OtpPurpose purpose, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        PhoneNumber = phoneNumber;
        Code = code;
        Purpose = purpose;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        AttemptCount = 0;
    }

    public bool Verify(string inputCode)
    {
        if (!CanAttempt)
            return false;

        AttemptCount++;
        
        if (Code == inputCode)
        {
            VerifiedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }
}

public enum OtpPurpose
{
    Registration,
    Login,
    PhoneNumberChange
}
