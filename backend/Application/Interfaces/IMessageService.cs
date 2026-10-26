using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Application.Interfaces;

public enum SendMessageStatus
{
    Sent,
    Duplicate,
    NotParticipant,
    InvalidContent,
    InvalidReply
}

public record SendMessageResult(
    SendMessageStatus Status,
    Message? Message = null,
    IReadOnlyList<Guid>? RecipientIds = null)
{
    public bool IsSuccess => Status is SendMessageStatus.Sent or SendMessageStatus.Duplicate;
}

/// <summary>A batch of messages from one sender in one conversation whose status changed.</summary>
public record MessageStatusChange(
    Guid ConversationId,
    Guid SenderId,
    IReadOnlyList<Guid> MessageIds,
    MessageStatus Status,
    DateTime At);

public interface IMessageService
{
    /// <summary>
    /// Persists a message. When clientMessageId was already used by this sender in this
    /// conversation, returns the existing message with Status = Duplicate instead of creating another.
    /// Also updates the conversation's last message and the unread counts of the other participants.
    /// </summary>
    Task<SendMessageResult> SendAsync(
        Guid conversationId,
        Guid senderId,
        MessageContent content,
        MessageType type,
        Guid? replyToMessageId,
        Guid? clientMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>Marks every Sent message addressed to the user as Delivered (used when the user connects).</summary>
    Task<IReadOnlyList<MessageStatusChange>> MarkDeliveredForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks messages up to and including lastReadMessageId as read for the user and resets their unread count.
    /// Returns null when the user is not a participant or the message is not in that conversation.
    /// </summary>
    Task<IReadOnlyList<MessageStatusChange>?> MarkReadAsync(
        Guid conversationId,
        Guid userId,
        Guid lastReadMessageId,
        CancellationToken cancellationToken = default);
}
