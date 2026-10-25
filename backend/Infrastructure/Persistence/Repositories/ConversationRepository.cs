using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Persistence.Repositories;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(ChatAppDbContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetConversationBetweenUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
    {
        // Find direct conversations where both users are participants
        var conversation = await _context.Conversations
            .Where(c => c.Type == ConversationType.Direct && !c.IsDeleted)
            .Where(c => c.Participants.Any(p => p.UserId == userId1 && !p.IsDeleted))
            .Where(c => c.Participants.Any(p => p.UserId == userId2 && !p.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return conversation;
    }
}

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetConversationBetweenUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
}