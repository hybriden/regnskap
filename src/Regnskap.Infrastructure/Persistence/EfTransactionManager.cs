using Microsoft.EntityFrameworkCore.Storage;
using Regnskap.Domain.Common;

namespace Regnskap.Infrastructure.Persistence;

/// <summary>
/// EF Core-implementasjon av ITransactionManager.
/// Wrapper rundt IDbContextTransaction.
/// </summary>
public class EfTransactionManager : ITransactionManager
{
    private readonly RegnskapDbContext _db;

    public EfTransactionManager(RegnskapDbContext db)
    {
        _db = db;
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _db.Database.BeginTransactionAsync(ct);
        return new EfTransactionScope(transaction);
    }

    private class EfTransactionScope : ITransactionScope
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransactionScope(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken ct = default)
            => await _transaction.CommitAsync(ct);

        public async Task RollbackAsync(CancellationToken ct = default)
            => await _transaction.RollbackAsync(ct);

        public async ValueTask DisposeAsync()
            => await _transaction.DisposeAsync();
    }
}
