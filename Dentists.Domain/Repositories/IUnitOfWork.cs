namespace Dentists.Domain.Repositories;

public interface IUnitOfWork
{
    IDentistRepository Dentists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a single database transaction, so that several
    /// SaveChanges calls commit atomically.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
