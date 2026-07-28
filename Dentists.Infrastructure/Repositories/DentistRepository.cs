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
    public async Task<Dentist?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Dentists.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
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
        // so this asks for dentists with no live appointment landing in the window.
        return await _context.Dentists
            .AsNoTracking()
            .Where(d => !d.Appointments.Any(a =>
                a.Status != Statuses.Cancelled
                && a.ScheduledDate >= from
                && a.ScheduledDate < to))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Dentists.AnyAsync(d => d.Id == id, cancellationToken);
    }
}
