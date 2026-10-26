using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    // User ID -> connection IDs.
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>>
        _userConnections = new();

    // Connection ID -> user ID.
    private static readonly ConcurrentDictionary<string, Guid>
        _connectionUserMap = new();

    public ChatHub(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        var connectionId = Context.ConnectionId;

        var connections = _userConnections.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<string, byte>());

        connections.TryAdd(connectionId, 0);
        _connectionUserMap[connectionId] = userId;

        await Groups.AddToGroupAsync(connectionId, $"user_{userId}");

        var conversations = await _unitOfWork.ConversationParticipants.FindAsync(
            cp => cp.UserId == userId && !cp.IsDeleted);

        foreach (var participant in conversations)
        {
            await Groups.AddToGroupAsync(
                connectionId,
                $"conversation_{participant.ConversationId}");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user != null)
        {
            user.UpdateStatus(UserStatus.Online);
            await _unitOfWork.SaveChangesAsync();

            await NotifyContactsOnlineStatus(userId, true);
        }

        // Messages sent while this user was offline are now delivered; tell their senders.
        var delivered = await _messageService.MarkDeliveredForUserAsync(userId);
        foreach (var change in delivered)
        {
            await Clients.Group($"user_{change.SenderId}")
                .SendAsync("MessageStatusChanged", ChatPayloads.StatusChanged(change));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        if (_connectionUserMap.TryRemove(connectionId, out var userId))
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.TryRemove(connectionId, out _);

                if (connections.IsEmpty &&
                    _userConnections.TryRemove(
                        new System.Collections.Generic.KeyValuePair<
                            Guid,
                            ConcurrentDictionary<string, byte>>(
                                userId,
                                connections)))
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);

                    if (user != null)
                    {
                        user.UpdateStatus(UserStatus.Offline);
                        await _unitOfWork.SaveChangesAsync();
                        await NotifyContactsOnlineStatus(userId, false);
                    }
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a text message. Pass a client-generated clientMessageId so a retry after a dropped
    /// connection returns the original message instead of creating a duplicate.
    /// </summary>
    public async Task<SendMessageAck> SendMessage(
        Guid conversationId,
        string content,
        Guid? replyToMessageId = null,
        Guid? clientMessageId = null)
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            return SendMessageAck.Failed("Unauthorized", clientMessageId);
        }

        var result = await _messageService.SendAsync(
            conversationId,
            userId,
            MessageContent.FromText(content ?? string.Empty),
            MessageType.Text,
            replyToMessageId,
            clientMessageId);

        if (!result.IsSuccess || result.Message == null)
        {
            return SendMessageAck.Failed(result.Status.ToString(), clientMessageId);
        }

        var message = result.Message;

        if (result.Status == SendMessageStatus.Duplicate)
        {
            return SendMessageAck.Ok(message, clientMessageId, isDuplicate: true);
        }

        var recipientIds = result.RecipientIds ?? Array.Empty<Guid>();

        // Delivered when every recipient has a live connection (for a direct chat, the other user).
        if (recipientIds.Count > 0 && recipientIds.All(IsUserOnline))
        {
            message.MarkAsDelivered();
            await _unitOfWork.SaveChangesAsync();
        }

        var sender = await _unitOfWork.Users.GetByIdAsync(userId);
        var senderName = sender?.DisplayName ?? sender?.PhoneNumber.Value ?? "Unknown";

        // Push through each participant's personal group (joined on connect) rather than the
        // conversation group, so a conversation created after a user connected still reaches them.
        var targets = recipientIds
            .Append(userId)
            .Select(id => $"user_{id}")
            .ToList();

        await Clients.Groups(targets)
            .SendAsync("MessageReceived", ChatPayloads.MessageReceived(message, senderName));

        return SendMessageAck.Ok(message, clientMessageId, isDuplicate: false);
    }

    public async Task MarkAsRead(
        Guid conversationId,
        Guid lastReadMessageId)
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            return;
        }

        var changes = await _messageService.MarkReadAsync(conversationId, userId, lastReadMessageId);

        if (changes == null)
        {
            await Clients.Caller.SendAsync("Error", "Invalid read receipt.");
            return;
        }

        foreach (var change in changes)
        {
            await Clients.Group($"user_{change.SenderId}")
                .SendAsync("MessageStatusChanged", ChatPayloads.StatusChanged(change));
        }

        // Lets the reader's other devices clear their unread badge.
        await Clients.Group($"user_{userId}")
            .SendAsync("MessagesRead", new
            {
                conversationId,
                userId,
                lastReadMessageId
            });
    }

    public async Task TypingStart(Guid conversationId)
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            return;
        }

        var participant =
            await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
                cp => cp.ConversationId == conversationId
                      && cp.UserId == userId
                      && !cp.IsDeleted);

        if (participant == null)
        {
            return;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", new
            {
                conversationId,
                userId,
                userName = user?.DisplayName
            });
    }

    public async Task TypingStop(Guid conversationId)
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            return;
        }

        var participant =
            await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
                cp => cp.ConversationId == conversationId
                      && cp.UserId == userId
                      && !cp.IsDeleted);

        if (participant == null)
        {
            return;
        }

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserStoppedTyping", new
            {
                conversationId,
                userId
            });
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            return;
        }

        var participant =
            await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
                cp => cp.ConversationId == conversationId
                      && cp.UserId == userId
                      && !cp.IsDeleted);

        if (participant != null)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"conversation_{conversationId}");
        }
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        var userId = GetUserIdFromContext();

        if (userId == Guid.Empty)
        {
            return;
        }

        var participant =
            await _unitOfWork.ConversationParticipants.FirstOrDefaultAsync(
                cp => cp.ConversationId == conversationId
                      && cp.UserId == userId
                      && !cp.IsDeleted);

        if (participant != null)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"conversation_{conversationId}");
        }
    }

    public async Task GetUsersOnlineStatus(Guid[] userIds)
    {
        var currentUserId = GetUserIdFromContext();

        if (currentUserId == Guid.Empty || userIds == null)
        {
            return;
        }

        var statuses = userIds
            .Distinct()
            .Select(userId => new
            {
                userId,
                isOnline = IsUserOnline(userId)
            })
            .ToArray();

        await Clients.Caller.SendAsync("UsersOnlineStatus", statuses);
    }

    private Guid GetUserIdFromContext()
    {
        var userIdClaim =
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirst("sub");

        return userIdClaim != null
               && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }

    private async Task NotifyContactsOnlineStatus(
        Guid userId,
        bool isOnline)
    {
        var conversations =
            await _unitOfWork.ConversationParticipants.FindAsync(
                cp => cp.UserId == userId && !cp.IsDeleted);

        var contactIds = new System.Collections.Generic.HashSet<Guid>();

        foreach (var conversation in conversations)
        {
            var participants =
                await _unitOfWork.ConversationParticipants.FindAsync(
                    cp => cp.ConversationId == conversation.ConversationId
                          && cp.UserId != userId
                          && !cp.IsDeleted);

            foreach (var participant in participants)
            {
                contactIds.Add(participant.UserId);
            }
        }

        foreach (var contactId in contactIds)
        {
            await Clients.Group($"user_{contactId}")
                .SendAsync("ContactOnlineStatusChanged", new
                {
                    userId,
                    isOnline
                });
        }
    }

    public static bool IsUserOnline(Guid userId)
    {
        return _userConnections.TryGetValue(userId, out var connections)
               && !connections.IsEmpty;
    }

    public static IReadOnlyCollection<string> GetUserConnectionIds(
        Guid userId)
    {
        return _userConnections.TryGetValue(userId, out var connections)
            ? connections.Keys.ToArray()
            : Array.Empty<string>();
    }
}

public static class ChatHubExtensions
{
    public static async Task SendToUserAsync(
        this IHubContext<ChatHub> hubContext,
        Guid userId,
        string method,
        object data)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync(method, data);
    }

    public static async Task SendToConversationAsync(
        this IHubContext<ChatHub> hubContext,
        Guid conversationId,
        string method,
        object data)
    {
        await hubContext.Clients.Group($"conversation_{conversationId}")
            .SendAsync(method, data);
    }
}