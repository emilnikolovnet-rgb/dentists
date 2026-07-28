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
    /// Stages a new dentist. Nothing reaches the store until the unit of work is saved.
    /// </summary>
    /// <remarks>
    /// There is no counterpart for removal: deleting a dentist is soft, done by calling
    /// <see cref="Dentist.MarkDeleted"/> and saving.
    /// </remarks>
    void Add(Dentist dentist);
}
