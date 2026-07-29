namespace Dentists.Infrastructure.Repositories;

using Dentists.Domain.Entities;
using Dentists.Domain.Enums;
using Dentists.Domain.Repositories;
using Dentists.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class DentistRepository : IDentistRepository
{
    private readonly DentistsDbContext _context;

    public DentistRepository(DentistsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Excludes soft-deleted dentists.
    /// <para>
    /// Spelled out per query rather than configured once as a model-wide query filter: such a
    /// filter wraps the query root, and the Cosmos provider then refuses WithPartitionKey.
    /// </para>
    /// <para>
    /// The IS_DEFINED half is not redundant. A document written before deletedDate existed has
    /// no such property, and comparing a missing property to null yields Undefined in Cosmos
    /// rather than true — so on that test alone every such dentist would read as deleted.
    /// </para>
    /// </summary>
    private static readonly System.Linq.Expressions.Expression<Func<Dentist, bool>> NotDeleted =
        d => !EF.Functions.IsDefined(d.DeletedDate) || d.DeletedDate == null;

    // Tracked on purpose: callers may mutate what this returns.
    public async Task<Dentist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Naming the partition key and matching only the key keeps this a ReadItem — one RU,
        // no query engine. Adding the soft-delete predicate here would disqualify that and
        // demote it to a SQL query, so the check happens below instead. It costs nothing
        // extra: either way exactly one document is fetched.
        var dentist = await _context.Dentists
            .WithPartitionKey(id)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return dentist is { DeletedDate: null } ? dentist : null;
    }

    public async Task<IEnumerable<Dentist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Dentists
            .AsNoTracking()
            .Where(NotDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Dentist>> GetAvailableAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        // Availability is the absence of a booking rather than a positive record of free time,
        // so this asks for dentists with no live appointment landing in the window. The
        // appointments are embedded, so it stays one cross-partition query with no join.
        return await _context.Dentists
            .AsNoTracking()
            .Where(NotDeleted)
            .Where(d => !d.Appointments.Any(a =>
                a.Status != Statuses.Cancelled
                && a.ScheduledDate >= from
                && a.ScheduledDate < to))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Dentists
            .WithPartitionKey(id)
            .Where(NotDeleted)
            .AnyAsync(d => d.Id == id, cancellationToken);
    }

    // Tracked: the reservation path may go on to mutate what this returns.
    public async Task<Dentist?> FindByAppointmentCorrelationIdAsync(
        Guid appointmentCorrelationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Dentists
            .Where(NotDeleted)
            .Where(d => d.Appointments.Any(a => a.AppointmentCorrelationId == appointmentCorrelationId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Tracked: the dispatcher marks these dispatched and saves.
    public async Task<IReadOnlyList<Dentist>> GetWithPendingOutboxAsync(
        int maxDentists,
        CancellationToken cancellationToken = default)
    {
        // No NotDeleted here, on purpose. A soft-deleted dentist can still be holding messages
        // its last changes committed to publishing, and dropping those would lose them.
        //
        // The IS_DEFINED half matters more here than anywhere else: if the provider ever omits
        // a null rather than writing it, "dispatchedAt = null" is Undefined and matches
        // nothing — and an outbox that quietly never drains looks exactly like an idle one.
        return await _context.Dentists
            .Where(d => d.Outbox.Any(m =>
                !EF.Functions.IsDefined(m.DispatchedAt) || m.DispatchedAt == null))
            .Take(maxDentists)
            .ToListAsync(cancellationToken);
    }

    public void Add(Dentist dentist)
    {
        _context.Dentists.Add(dentist);
    }
}
