using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Infrastructure.Services;

public class MessageService : IMessageService
{
    public const int MaxTextLength = 4096;

    private readonly IUnitOfWork _unitOfWork;

    public MessageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SendMessageResult> SendAsync(
        Guid conversationId,
        Guid senderId,
        MessageContent content,
        MessageType type,
        Guid? replyToMessageId,
        Guid? clientMessageId,
        CancellationToken cancellationToken = default)
    {
        var hasContent = content.HasText || content.HasMedia || content.HasLocation || content.HasContact;
        if (!hasContent || (content.Text?.Length ?? 0) > MaxTextLength)
        {
            return new SendMessageResult(SendMessageStatus.InvalidContent);
        }

        var participants = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.ConversationId == conversationId && !cp.IsDeleted,
            cancellationToken);

        var senderParticipant = participants.FirstOrDefault(p => p.UserId == senderId);
        if (senderParticipant == null)
        {
            return new SendMessageResult(SendMessageStatus.NotParticipant);
        }

        var recipientIds = participants
            .Where(p => p.UserId != senderId)
            .Select(p => p.UserId)
            .ToList();

        if (clientMessageId.HasValue)
        {
            var existing = await FindByClientMessageIdAsync(
                conversationId, senderId, clientMessageId.Value, cancellationToken);

            if (existing != null)
            {
                return new SendMessageResult(SendMessageStatus.Duplicate, existing, recipientIds);
            }
        }

        if (replyToMessageId.HasValue)
        {
            var replyId = replyToMessageId.Value;
            var replyExists = await _unitOfWork.Messages.ExistsAsync(
                m => m.Id == replyId && m.ConversationId == conversationId,
                cancellationToken);

            if (!replyExists)
            {
                return new SendMessageResult(SendMessageStatus.InvalidReply);
            }
        }

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            return new SendMessageResult(SendMessageStatus.NotParticipant);
        }

        var message = new Message(
            conversationId,
            senderId,
            content,
            type,
            replyToMessageId,
            clientMessageId);

        await _unitOfWork.Messages.AddAsync(message, cancellationToken);

        conversation.UpdateLastMessage(message.Id, message.SentAt);
        senderParticipant.MarkAsRead(message.SentAt);

        foreach (var participant in participants)
        {
            if (participant.UserId != senderId)
            {
                participant.IncrementUnreadCount();
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (clientMessageId.HasValue)
        {
            // Two requests with the same clientMessageId raced and the unique index rejected ours.
            // Nothing from this attempt was saved, so return the message that won.
            var existing = await FindByClientMessageIdAsync(
                conversationId, senderId, clientMessageId.Value, cancellationToken);

            if (existing == null)
            {
                throw;
            }

            return new SendMessageResult(SendMessageStatus.Duplicate, existing, recipientIds);
        }

        return new SendMessageResult(SendMessageStatus.Sent, message, recipientIds);
    }

    public async Task<IReadOnlyList<MessageStatusChange>> MarkDeliveredForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.UserId == userId && !cp.IsDeleted,
            cancellationToken);

        var conversationIds = memberships.Select(cp => cp.ConversationId).ToList();
        if (conversationIds.Count == 0)
        {
            return Array.Empty<MessageStatusChange>();
        }

        var pending = await _unitOfWork.Messages.FindAsync(
            m => conversationIds.Contains(m.ConversationId)
                 && m.SenderId != userId
                 && m.Status == MessageStatus.Sent,
            cancellationToken);

        if (pending.Count == 0)
        {
            return Array.Empty<MessageStatusChange>();
        }

        foreach (var message in pending)
        {
            message.MarkAsDelivered();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return pending
            .GroupBy(m => new { m.ConversationId, m.SenderId })
            .Select(g => new MessageStatusChange(
                g.Key.ConversationId,
                g.Key.SenderId,
                g.Select(m => m.Id).ToList(),
                MessageStatus.Delivered,
                now))
            .ToList();
    }

    public async Task<IReadOnlyList<MessageStatusChange>?> MarkReadAsync(
        Guid conversationId,
        Guid userId,
        Guid lastReadMessageId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == conversationId && cp.UserId == userId && !cp.IsDeleted,
            cancellationToken);

        if (participant == null)
        {
            return null;
        }

        var lastRead = await _unitOfWork.Messages.FirstOrDefaultAsync(
            m => m.Id == lastReadMessageId && m.ConversationId == conversationId,
            cancellationToken);

        if (lastRead == null)
        {
            return null;
        }

        var upTo = lastRead.SentAt;
        participant.MarkAsRead(upTo);

        var unread = await _unitOfWork.Messages.FindAsync(
            m => m.ConversationId == conversationId
                 && m.SenderId != userId
                 && m.SentAt <= upTo
                 && (m.Status == MessageStatus.Sent || m.Status == MessageStatus.Delivered),
            cancellationToken);

        foreach (var message in unread)
        {
            message.MarkAsRead();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return unread
            .GroupBy(m => m.SenderId)
            .Select(g => new MessageStatusChange(
                conversationId,
                g.Key,
                g.Select(m => m.Id).ToList(),
                MessageStatus.Read,
                now))
            .ToList();
    }

    private Task<Message?> FindByClientMessageIdAsync(
        Guid conversationId,
        Guid senderId,
        Guid clientMessageId,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.Messages.FirstOrDefaultAsync(
            m => m.ConversationId == conversationId
                 && m.SenderId == senderId
                 && m.ClientMessageId == clientMessageId,
            cancellationToken);
    }
}
