namespace Dentists.Domain.Repositories;

using Dentists.Domain.Entities;

public interface IDentistRepository
{
    /// <summary>
    /// Loads a tracked dentist, so callers may mutate it and persist through the unit of work.
    /// </summary>
    Task<Dentist?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Dentist>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dentists with no live booking inside [<paramref name="from"/>, <paramref name="to"/>).
    /// Cancelled appointments do not make a dentist unavailable.
    /// </summary>
    Task<IEnumerable<Dentist>> GetAvailableAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
