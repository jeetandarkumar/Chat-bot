using System;
using System.Threading;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetConversationBetweenUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
}
