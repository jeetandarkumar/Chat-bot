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
[Route("api/contacts")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ContactsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Get conversations where user is a participant (direct chats = contacts)
        var participants = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.UserId == userId && !cp.IsDeleted);

        var contactIds = new List<Guid>();
        foreach (var p in participants)
        {
            var conv = await _unitOfWork.Conversations.GetByIdAsync(p.ConversationId);
            if (conv != null && conv.Type == ConversationType.Direct && !conv.IsDeleted)
            {
                var otherParticipant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
                    cp => cp.ConversationId == conv.Id && cp.UserId != userId && !cp.IsDeleted);
                if (otherParticipant != null)
                {
                    contactIds.Add(otherParticipant.UserId);
                }
            }
        }

        var contacts = new List<object>();
        foreach (var contactId in contactIds.Distinct())
        {
            var user = await _unitOfWork.Users.GetByIdAsync(contactId);
            if (user != null && !user.IsDeleted)
            {
                // Get the conversation ID for this contact
                var conv = await _unitOfWork.Conversations.GetConversationBetweenUsersAsync(userId, contactId);

                contacts.Add(new
                {
                    id = user.Id,
                    displayName = user.DisplayName,
                    phoneNumber = user.PhoneNumber.Value,
                    profilePictureUrl = user.ProfilePictureUrl,
                    status = user.Status.ToString(),
                    lastSeenAt = user.LastSeenAt,
                    conversationId = conv?.Id
                });
            }
        }

        // Paginate
        contacts = contacts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(contacts);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest(new { message = "Search query must be at least 2 characters" });
        }

        // Search by phone number or display name
        var users = await _unitOfWork.Users.FindAsync(
            u => !u.IsDeleted && 
                 u.Id != userId && 
                 (u.PhoneNumber.Value.Contains(query) || 
                  (u.DisplayName != null && u.DisplayName.Contains(query))));

        var result = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                id = u.Id,
                displayName = u.DisplayName,
                phoneNumber = u.PhoneNumber.Value,
                profilePictureUrl = u.ProfilePictureUrl,
                status = u.Status.ToString(),
                lastSeenAt = u.LastSeenAt
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetContact(Guid userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        if (currentUserId == userId)
        {
            return BadRequest(new { message = "Cannot get yourself as a contact" });
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if there's an existing direct conversation
        var conversation = await _unitOfWork.Conversations.GetConversationBetweenUsersAsync(currentUserId, userId);

        return Ok(new
        {
            id = user.Id,
            displayName = user.DisplayName,
            phoneNumber = user.PhoneNumber.Value,
            profilePictureUrl = user.ProfilePictureUrl,
            status = user.Status.ToString(),
            lastSeenAt = user.LastSeenAt,
            conversationId = conversation?.Id
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrGetContact([FromBody] CreateContactRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Find user by phone number
        var users = await _unitOfWork.Users.FindAsync(
            u => u.PhoneNumber.Value == request.PhoneNumber && !u.IsDeleted);
        
        var contact = users.FirstOrDefault();
        if (contact == null)
        {
            return NotFound(new { message = "User not found with this phone number" });
        }

        if (contact.Id == userId)
        {
            return BadRequest(new { message = "Cannot add yourself as a contact" });
        }

        // Check if direct conversation already exists
        var existingConversation = await _unitOfWork.Conversations.GetConversationBetweenUsersAsync(userId, contact.Id);
        if (existingConversation != null)
        {
            return Ok(new
            {
                id = contact.Id,
                displayName = contact.DisplayName,
                phoneNumber = contact.PhoneNumber.Value,
                profilePictureUrl = contact.ProfilePictureUrl,
                status = contact.Status.ToString(),
                lastSeenAt = contact.LastSeenAt,
                conversationId = existingConversation.Id,
                isNew = false
            });
        }

        // Create new direct conversation
        var conversation = new Conversation(ConversationType.Direct, new[] { userId, contact.Id });
        await _unitOfWork.Conversations.AddAsync(conversation);

        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContact), new { userId = contact.Id }, new
        {
            id = contact.Id,
            displayName = contact.DisplayName,
            phoneNumber = contact.PhoneNumber.Value,
            profilePictureUrl = contact.ProfilePictureUrl,
            status = contact.Status.ToString(),
            lastSeenAt = contact.LastSeenAt,
            conversationId = conversation.Id,
            isNew = true
        });
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveContact(Guid userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var conversation = await _unitOfWork.Conversations.GetConversationBetweenUsersAsync(currentUserId, userId);
        if (conversation == null)
        {
            return NotFound(new { message = "Contact not found" });
        }

        // Leave the conversation (soft delete participant)
        var participant = await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == conversation.Id && cp.UserId == currentUserId && !cp.IsDeleted);

        if (participant != null)
        {
            participant.Leave();
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(new { message = "Contact removed" });
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

public record CreateContactRequest(string PhoneNumber);