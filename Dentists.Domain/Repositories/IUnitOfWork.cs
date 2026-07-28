namespace Dentists.Domain.Repositories;

public interface IUnitOfWork
{
    IDentistRepository Dentists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> and commits everything it changed in one save.
    /// <para>
    /// Atomicity extends only as far as a single dentist: Cosmos commits per partition key,
    /// so changes touching two dentists are two batches and either may fail alone.
    /// </para>
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
