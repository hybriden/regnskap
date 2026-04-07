using Regnskap.Domain.Common;

namespace Regnskap.Tests.Features.Bilagsregistrering;

/// <summary>
/// In-memory fake transaction manager for unit tests.
/// Transaksjoner er no-ops i tester siden vi bruker fake repositories.
/// </summary>
public class FakeTransactionManager : ITransactionManager
{
    public int TransactionCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        TransactionCount++;
        return Task.FromResult<ITransactionScope>(new FakeTransactionScope(this));
    }

    private class FakeTransactionScope : ITransactionScope
    {
        private readonly FakeTransactionManager _manager;

        public FakeTransactionScope(FakeTransactionManager manager) => _manager = manager;

        public Task CommitAsync(CancellationToken ct = default)
        {
            _manager.CommitCount++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken ct = default)
        {
            _manager.RollbackCount++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
