namespace Dentists.Domain.Repositories;

public interface IUnitOfWork
{
    IDentistRepository Dentists { get; }

    /// <summary>
    /// Commits everything staged since the last save.
    /// <para>
    /// Atomicity extends only as far as a single dentist: Cosmos commits per partition key, so
    /// EF batches changes to one dentist — its appointments, its outbox and its inbox — into a
    /// single transactional write, while changes spanning two dentists are two batches and
    /// either may fail alone. Keeping a unit of work to one dentist is what makes the outbox
    /// pattern hold on this store.
    /// </para>
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
