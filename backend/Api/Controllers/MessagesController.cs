using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Api.Hubs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:guid}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        IHubContext<ChatHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? beforeMessageId = null)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Verify user is a participant
        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == conversationId && cp.UserId == userId && !cp.IsDeleted);

        if (participant == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        // Build query
        var messages = await _unitOfWork.Messages.FindAsync(
            m => m.ConversationId == conversationId && !m.IsDeleted);

        // Filter by beforeMessageId if provided (for pagination)
        if (beforeMessageId.HasValue)
        {
            var beforeMessage = await _unitOfWork.Messages.GetByIdAsync(beforeMessageId.Value);
            if (beforeMessage != null)
            {
                messages = messages.Where(m => m.CreatedAt < beforeMessage.CreatedAt).ToList();
            }
        }

        // Sort by timestamp descending (newest first), then paginate
        messages = messages
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Reverse to show oldest first in the returned page
        messages.Reverse();

        var result = new List<object>();
        foreach (var msg in messages)
        {
            var sender = await _unitOfWork.Users.GetByIdAsync(msg.SenderId);
            
            object? replyTo = null;
            if (msg.ReplyToMessageId.HasValue)
            {
                var replyMsg = await _unitOfWork.Messages.GetByIdAsync(msg.ReplyToMessageId.Value);
                if (replyMsg != null)
                {
                    var replySender = await _unitOfWork.Users.GetByIdAsync(replyMsg.SenderId);
                    replyTo = new
                    {
                        id = replyMsg.Id,
                        content = replyMsg.Content.Text,
                        senderName = replySender?.DisplayName ?? "Unknown"
                    };
                }
            }

            result.Add(new
            {
                id = msg.Id,
                conversationId = msg.ConversationId,
                senderId = msg.SenderId,
                senderName = sender?.DisplayName ?? sender?.PhoneNumber.Value ?? "Unknown",
                content = msg.Content.Text,
                messageType = msg.Type.ToString(),
                replyTo = replyTo,
                timestamp = msg.CreatedAt,
                status = msg.Status.ToString(),
                isOwn = msg.SenderId == userId
            });
        }

        return Ok(new
        {
            messages = result,
            hasMore = messages.Count == pageSize
        });
    }

    [HttpGet("{messageId:guid}")]
    public async Task<IActionResult> GetMessage(Guid conversationId, Guid messageId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == conversationId && cp.UserId == userId && !cp.IsDeleted);

        if (participant == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null || message.ConversationId != conversationId || message.IsDeleted)
        {
            return NotFound(new { message = "Message not found" });
        }

        var sender = await _unitOfWork.Users.GetByIdAsync(message.SenderId);
        
        object? replyTo = null;
        if (message.ReplyToMessageId.HasValue)
        {
            var replyMsg = await _unitOfWork.Messages.GetByIdAsync(message.ReplyToMessageId.Value);
            if (replyMsg != null)
            {
                var replySender = await _unitOfWork.Users.GetByIdAsync(replyMsg.SenderId);
                replyTo = new
                {
                    id = replyMsg.Id,
                    content = replyMsg.Content.Text,
                    senderName = replySender?.DisplayName ?? "Unknown"
                };
            }
        }

        return Ok(new
        {
            id = message.Id,
            conversationId = message.ConversationId,
            senderId = message.SenderId,
            senderName = sender?.DisplayName ?? sender?.PhoneNumber.Value ?? "Unknown",
            content = message.Content.Text,
            messageType = message.Type.ToString(),
            replyTo = replyTo,
            timestamp = message.CreatedAt,
            status = message.Status.ToString(),
            isOwn = message.SenderId == userId
        });
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Message content cannot be empty" });
        }

        var result = await _messageService.SendAsync(
            conversationId,
            userId,
            MessageContent.FromText(request.Content),
            request.MessageType,
            request.ReplyToMessageId,
            request.ClientMessageId);

        if (result.Status == SendMessageStatus.NotParticipant)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        if (result.Status == SendMessageStatus.InvalidContent)
        {
            return BadRequest(new { message = "Message content is empty or too long" });
        }

        if (result.Status == SendMessageStatus.InvalidReply)
        {
            return BadRequest(new { message = "Reply target not found in this conversation" });
        }

        var message = result.Message!;
        var sender = await _unitOfWork.Users.GetByIdAsync(userId);
        var senderName = sender?.DisplayName ?? sender?.PhoneNumber.Value ?? "Unknown";

        var body = new
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

        if (result.Status == SendMessageStatus.Duplicate)
        {
            return Ok(body);
        }

        // Push to every participant's connected devices, same as a message sent through the hub.
        var targets = (result.RecipientIds ?? Array.Empty<Guid>())
            .Append(userId)
            .Select(id => $"user_{id}")
            .ToList();

        await _hubContext.Clients.Groups(targets)
            .SendAsync("MessageReceived", ChatPayloads.MessageReceived(message, senderName));

        return CreatedAtAction(nameof(GetMessage), new { conversationId, messageId = message.Id }, body);
    }

    [HttpPut("{messageId:guid}")]
    public async Task<IActionResult> EditMessage(Guid conversationId, Guid messageId, [FromBody] EditMessageRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null || message.ConversationId != conversationId || message.IsDeleted)
        {
            return NotFound(new { message = "Message not found" });
        }

        if (message.SenderId != userId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Message content cannot be empty" });
        }

        // Note: Message entity doesn't have UpdateContent method, so we'll need to create a new MessageContent
        // Since Content is a record with init-only properties, we need to recreate it
        // The Message entity would need an UpdateContent method - for now let's just return an error
        return BadRequest(new { message = "Editing messages is not yet supported" });
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid conversationId, Guid messageId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null || message.ConversationId != conversationId || message.IsDeleted)
        {
            return NotFound(new { message = "Message not found" });
        }

        if (message.SenderId != userId)
        {
            return Forbid();
        }

        message.SoftDelete();
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Message deleted" });
    }

    [HttpPost("{messageId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid conversationId, Guid messageId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var changes = await _messageService.MarkReadAsync(conversationId, userId, messageId);
        if (changes == null)
        {
            return NotFound(new { message = "Conversation or message not found" });
        }

        foreach (var change in changes)
        {
            await _hubContext.Clients.Group($"user_{change.SenderId}")
                .SendAsync("MessageStatusChanged", ChatPayloads.StatusChanged(change));
        }

        return Ok(new { message = "Marked as read" });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}

public record SendMessageRequest(
    string Content,
    MessageType MessageType = MessageType.Text,
    Guid? ReplyToMessageId = null,
    Guid? ClientMessageId = null
);

public record EditMessageRequest(
    string Content
);