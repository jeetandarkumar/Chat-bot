using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ConversationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        // Get conversations where user is a participant
        var participants = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.UserId == userId && !cp.IsDeleted);

        var conversationIds = participants.Select(p => p.ConversationId).ToList();

        var conversations = new List<Conversation>();
        foreach (var convId in conversationIds)
        {
            var conv = await _unitOfWork.Conversations.GetByIdAsync(convId);
            if (conv != null && !conv.IsDeleted)
            {
                conversations.Add(conv);
            }
        }

        // Sort by last message timestamp descending
        conversations = conversations
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<object>();
        foreach (var conv in conversations)
        {
            var otherParticipants = await _unitOfWork.ConversationParticipants.FindAsync(
                cp => cp.ConversationId == conv.Id && cp.UserId != userId && !cp.IsDeleted);

            var otherUsers = new List<object>();
            foreach (var p in otherParticipants)
            {
                var u = await _unitOfWork.Users.GetByIdAsync(p.UserId);
                if (u != null)
                {
                    otherUsers.Add(new
                    {
                        id = u.Id,
                        displayName = u.DisplayName,
                        phoneNumber = u.PhoneNumber.Value,
                        profilePictureUrl = u.ProfilePictureUrl,
                        status = u.Status.ToString(),
                        lastSeenAt = u.LastSeenAt
                    });
                }
            }

            // Calculate unread count for this conversation
            var myParticipant = participants.FirstOrDefault(p => p.ConversationId == conv.Id);
            var unreadCount = myParticipant?.UnreadCount ?? 0;

            result.Add(new
            {
                id = conv.Id,
                type = conv.Type.ToString(),
                name = conv.Name,
                avatarUrl = conv.AvatarUrl,
                lastMessageAt = conv.LastMessageAt,
                lastMessageId = conv.LastMessageId,
                participants = otherUsers,
                unreadCount
            });
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == id && cp.UserId == userId && !cp.IsDeleted);

        if (participant == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(id);
        if (conversation == null || conversation.IsDeleted)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        var otherParticipants = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.ConversationId == id && cp.UserId != userId && !cp.IsDeleted);

        var otherUsers = new List<object>();
        foreach (var p in otherParticipants)
        {
            var u = await _unitOfWork.Users.GetByIdAsync(p.UserId);
            if (u != null)
            {
                otherUsers.Add(new
                {
                    id = u.Id,
                    displayName = u.DisplayName,
                    phoneNumber = u.PhoneNumber.Value,
                    profilePictureUrl = u.ProfilePictureUrl,
                    status = u.Status.ToString(),
                    lastSeenAt = u.LastSeenAt
                });
            }
        }

        return Ok(new
        {
            id = conversation.Id,
            type = conversation.Type.ToString(),
            name = conversation.Name,
            avatarUrl = conversation.AvatarUrl,
            lastMessageAt = conversation.LastMessageAt,
            lastMessageId = conversation.LastMessageId,
            participants = otherUsers,
            createdAt = conversation.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (request.ParticipantIds == null || request.ParticipantIds.Length == 0)
        {
            return BadRequest(new { message = "At least one participant is required" });
        }

        // Check if all participants exist
        var participants = new List<User>();
        foreach (var pid in request.ParticipantIds)
        {
            var u = await _unitOfWork.Users.GetByIdAsync(pid);
            if (u == null || u.IsDeleted)
            {
                return NotFound(new { message = $"User {pid} not found" });
            }
            participants.Add(u);
        }

        // If only one participant and it's a direct chat, check if conversation already exists
        if (participants.Count == 1 && request.Type == ConversationType.Direct)
        {
            var existing = await FindExistingDirectConversation(userId, participants[0].Id);
            if (existing != null)
            {
                return Ok(new { id = existing.Id, message = "Conversation already exists" });
            }
        }

        // Create conversation
        var conversation = new Conversation(
            request.Type,
            request.ParticipantIds,
            request.Name,
            request.AvatarUrl);

        await _unitOfWork.Conversations.AddAsync(conversation);

        // Add current user as participant (admin for groups)
        var currentUserParticipant = new ConversationParticipant(conversation.Id, userId, request.Type == ConversationType.Group);
        await _unitOfWork.ConversationParticipants.AddAsync(currentUserParticipant);

        // Add other participants
        foreach (var u in participants)
        {
            var participant = new ConversationParticipant(conversation.Id, u.Id, false);
            await _unitOfWork.ConversationParticipants.AddAsync(participant);
        }

        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, new
        {
            id = conversation.Id,
            type = conversation.Type.ToString(),
            name = conversation.Name,
            avatarUrl = conversation.AvatarUrl
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == id && cp.UserId == userId && !cp.IsDeleted);

        if (participant == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        // Only admin can delete group conversations
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(id);
        if (conversation != null && conversation.Type == ConversationType.Group && !participant.IsAdmin)
        {
            return Forbid();
        }

        // Soft delete - mark participant as deleted (leave conversation)
        participant.Leave();
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Left conversation" });
    }

    [HttpPost("{id:guid}/participants")]
    public async Task<IActionResult> AddParticipant(Guid id, [FromBody] AddParticipantRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == id && cp.UserId == userId && !cp.IsDeleted);

        if (participant == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        if (!participant.IsAdmin)
        {
            return Forbid();
        }

        var userToAdd = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (userToAdd == null || userToAdd.IsDeleted)
        {
            return NotFound(new { message = "User to add not found" });
        }

        // Check if already a participant
        var existing = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == id && cp.UserId == request.UserId && !cp.IsDeleted);
        if (existing != null)
        {
            return Conflict(new { message = "User is already a participant" });
        }

        var newParticipant = new ConversationParticipant(id, request.UserId, false);
        await _unitOfWork.ConversationParticipants.AddAsync(newParticipant);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Participant added" });
    }

    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var currentParticipant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == id && cp.UserId == currentUserId && !cp.IsDeleted);

        if (currentParticipant == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        // Users can remove themselves, admins can remove others
        if (currentUserId != userId && !currentParticipant.IsAdmin)
        {
            return Forbid();
        }

        var participantToRemove = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == id && cp.UserId == userId && !cp.IsDeleted);

        if (participantToRemove == null)
        {
            return NotFound(new { message = "Participant not found" });
        }

        participantToRemove.Leave();
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Participant removed" });
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

    private async Task<Conversation?> FindExistingDirectConversation(Guid userId1, Guid userId2)
    {
        var user1Conversations = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.UserId == userId1 && !cp.IsDeleted);

        foreach (var cp in user1Conversations)
        {
            var conv = await _unitOfWork.Conversations.GetByIdAsync(cp.ConversationId);
            if (conv != null && conv.Type == ConversationType.Direct && !conv.IsDeleted)
            {
                var otherParticipant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
                    p => p.ConversationId == conv.Id && p.UserId == userId2 && !p.IsDeleted);
                if (otherParticipant != null)
                {
                    return conv;
                }
            }
        }
        return null;
    }
}

public record CreateConversationRequest(
    ConversationType Type,
    string? Name,
    string? AvatarUrl,
    Guid[] ParticipantIds
);

public record AddParticipantRequest(Guid UserId);