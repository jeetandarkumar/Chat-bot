using System;
using System.Collections.Generic;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Domain.Entities;

public class Conversation
{
    public Guid Id { get; private set; }
    public ConversationType Type { get; private set; }
    public string? Name { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public Guid? LastMessageId { get; private set; }

    private readonly List<ConversationParticipant> _participants = new();
    public IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();
    public ICollection<Message> Messages { get; private set; } = new List<Message>();

    private Conversation() { } // EF Core

    public Conversation(ConversationType type, IEnumerable<Guid> participantIds, string? name = null, string? avatarUrl = null)
    {
        Id = Guid.NewGuid();
        Type = type;
        Name = name;
        AvatarUrl = avatarUrl;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        
        foreach (var participantId in participantIds)
        {
            _participants.Add(new ConversationParticipant(Id, participantId));
        }
    }

    public void AddParticipant(Guid userId)
    {
        if (!_participants.Any(p => p.UserId == userId))
        {
            _participants.Add(new ConversationParticipant(Id, userId));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveParticipant(Guid userId)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            _participants.Remove(participant);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateLastMessage(Guid messageId, DateTime sentAt)
    {
        LastMessageId = messageId;
        LastMessageAt = sentAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string? name, string? avatarUrl)
    {
        Name = name;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsParticipant(Guid userId) => _participants.Any(p => p.UserId == userId);
}

public enum ConversationType
{
    Direct,
    Group
}