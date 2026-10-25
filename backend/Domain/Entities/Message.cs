using System;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Domain.Entities;

public class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public MessageContent Content { get; private set; } = new();
    public MessageType Type { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public Guid? ReplyToMessageId { get; private set; }

    // Navigation properties
    public Conversation Conversation { get; private set; } = null!;
    public User Sender { get; private set; } = null!;
    public Message? ReplyToMessage { get; private set; }

    private Message() { } // EF Core

    public Message(Guid conversationId, Guid senderId, MessageContent content, MessageType type, Guid? replyToMessageId = null)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        SenderId = senderId;
        Content = content;
        Type = type;
        Status = MessageStatus.Sent;
        SentAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        ReplyToMessageId = replyToMessageId;
    }

    public void MarkAsDelivered()
    {
        if (Status == MessageStatus.Sent)
        {
            Status = MessageStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkAsRead()
    {
        if (Status == MessageStatus.Sent || Status == MessageStatus.Delivered)
        {
            Status = MessageStatus.Read;
            ReadAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum MessageType
{
    Text,
    Image,
    Video,
    File,
    Audio,
    Location,
    Contact
}

public enum MessageStatus
{
    Sent,
    Delivered,
    Read,
    Failed
}
