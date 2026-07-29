namespace Dentists.Domain.Repositories;

using Dentists.Domain.Entities;

public interface IDentistRepository
{
    /// <summary>
    /// Loads a tracked dentist, so callers may mutate it and persist through the unit of work.
    /// </summary>
    Task<Dentist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Dentist>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dentists with no live booking inside [<paramref name="from"/>, <paramref name="to"/>).
    /// Cancelled appointments do not make a dentist unavailable.
    /// </summary>
    Task<IEnumerable<Dentist>> GetAvailableAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The dentist holding a given booking, or null if nobody does.
    /// <para>
    /// A cross-partition query — the booking id is not the partition key — so reserve for the
    /// reservation path, where it is what stops a redelivered reservation booking the same
    /// appointment with a second dentist.
    /// </para>
    /// </summary>
    Task<Dentist?> FindByAppointmentCorrelationIdAsync(
        Guid appointmentCorrelationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dentists with at least one outbox message still waiting to be published, tracked so the
    /// caller can mark them dispatched.
    /// <para>
    /// Deliberately includes soft-deleted dentists: deleting one must not strand messages its
    /// earlier changes already committed to publishing.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Dentist>> GetWithPendingOutboxAsync(
        int maxDentists,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new dentist. Nothing reaches the store until the unit of work is saved.
    /// </summary>
    /// <remarks>
    /// There is no counterpart for removal: deleting a dentist is soft, done by calling
    /// <see cref="Dentist.MarkDeleted"/> and saving.
    /// </remarks>
    void Add(Dentist dentist);
}
