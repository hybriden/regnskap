namespace Regnskap.Domain.Common;

/// <summary>
/// Abstraksjon for databasetransaksjoner.
/// Muliggjor eksplisitt transaksjonsstyring uten direkte EF Core-avhengighet i Application-laget.
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Starter en ny databasetransaksjon.
    /// </summary>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default);
}

/// <summary>
/// Representerer en aktiv databasetransaksjon.
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
