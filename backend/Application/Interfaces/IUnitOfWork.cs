using System;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Conversation> Conversations { get; }
    IRepository<Message> Messages { get; }
    IRepository<ConversationParticipant> ConversationParticipants { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<OtpCode> OtpCodes { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}