using System;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;

namespace ChatApp.Api.Hubs;

/// <summary>Shapes of the objects pushed to clients, shared by the hub and the REST controllers.</summary>
public static class ChatPayloads
{
    public static object MessageReceived(Message message, string senderName) => new
    {
        id = message.Id,
        clientMessageId = message.ClientMessageId,
        conversationId = message.ConversationId,
        senderId = message.SenderId,
        senderName,
        content = message.Content.Text,
        messageType = message.Type.ToString(),
        replyToMessageId = message.ReplyToMessageId,
        timestamp = message.CreatedAt,
        status = message.Status.ToString()
    };

    public static object StatusChanged(MessageStatusChange change) => new
    {
        conversationId = change.ConversationId,
        messageIds = change.MessageIds,
        status = change.Status.ToString(),
        at = change.At
    };
}

/// <summary>Result returned to the caller of ChatHub.SendMessage.</summary>
public record SendMessageAck(
    bool Success,
    string? Error,
    Guid? MessageId,
    Guid? ClientMessageId,
    string? Status,
    DateTime? Timestamp,
    bool IsDuplicate)
{
    public static SendMessageAck Ok(Message message, Guid? clientMessageId, bool isDuplicate) =>
        new(true, null, message.Id, clientMessageId, message.Status.ToString(), message.CreatedAt, isDuplicate);

    public static SendMessageAck Failed(string error, Guid? clientMessageId) =>
        new(false, error, null, clientMessageId, null, null, false);
}
