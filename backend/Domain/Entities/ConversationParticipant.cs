using System;

namespace ChatApp.Domain.Entities;

public class ConversationParticipant
{
    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsMuted { get; private set; }
    public DateTime? MutedUntil { get; private set; }
    public int UnreadCount { get; private set; }
    public DateTime? LastReadAt { get; private set; }
    public bool IsDeleted {get; private set;}

    // Navigation properties
    public Conversation Conversation { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ConversationParticipant() { } // EF Core

    public ConversationParticipant(Guid conversationId, Guid userId, bool isAdmin = false)
    {
        ConversationId = conversationId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
        IsAdmin = isAdmin;
        IsMuted = false;
        UnreadCount = 0;
    }

    public void IncrementUnreadCount()
    {
        UnreadCount++;
    }

    public void MarkAsRead(DateTime readAt)
    {
        UnreadCount = 0;
        LastReadAt = readAt;
    }

    public void Mute(DateTime? until = null)
    {
        IsMuted = true;
        MutedUntil = until;
    }

    public void Unmute()
    {
        IsMuted = false;
        MutedUntil = null;
    }

    public void Leave()
    {
        LeftAt = DateTime.UtcNow;
    }

    public bool IsActive => LeftAt == null;
}