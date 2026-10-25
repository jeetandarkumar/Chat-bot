using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Persistence.Repositories;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ChatAppDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public IRepository<User> Users { get; }
    public IRepository<Conversation> Conversations { get; }
    public IRepository<Message> Messages { get; }
    public IRepository<ConversationParticipant> ConversationParticipants { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }
    public IRepository<OtpCode> OtpCodes { get; }

    public UnitOfWork(ChatAppDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Conversations = new Repository<Conversation>(context);
        Messages = new Repository<Message>(context);
        ConversationParticipants = new Repository<ConversationParticipant>(context);
        RefreshTokens = new Repository<RefreshToken>(context);
        OtpCodes = new Repository<OtpCode>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}