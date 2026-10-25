using System;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }
    public string? DisplayName { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsPhoneNumberVerified { get; private set; }
    
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { } // EF Core

    public User(PhoneNumber phoneNumber, string? displayName = null)
    {
        Id = Guid.NewGuid();
        PhoneNumber = phoneNumber;
        DisplayName = displayName;
        Status = UserStatus.Offline;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        IsPhoneNumberVerified = false;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfilePicture(string? url)
    {
        ProfilePictureUrl = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPhoneNumberAsVerified()
    {
        IsPhoneNumberVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(UserStatus status)
    {
        Status = status;
        if (status == UserStatus.Offline)
        {
            LastSeenAt = DateTime.UtcNow;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRefreshToken(RefreshToken token)
    {
        _refreshTokens.Add(token);
    }

    public void RevokeRefreshToken(string tokenId)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
        if (token != null)
        {
            token.Revoke();
        }
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum UserStatus
{
    Online,
    Offline,
    Away
}
