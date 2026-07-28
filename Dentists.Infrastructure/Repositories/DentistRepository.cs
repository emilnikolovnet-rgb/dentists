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

    // Tracked on purpose: callers may mutate what this returns.
    public async Task<Dentist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Naming the partition key turns this into a point read against a single partition
        // rather than a fan-out across all of them.
        return await _context.Dentists
            .WithPartitionKey(id)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Dentist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Dentists
            .AsNoTracking()
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
            .AnyAsync(d => d.Id == id, cancellationToken);
    }
}
