namespace Dentists.Infrastructure.Repositories;

using Dentists.Domain.Repositories;
using Dentists.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class UnitOfWork : IUnitOfWork
{
    private readonly DentistsDbContext _context;
    private IDentistRepository? _dentistRepository;

    public UnitOfWork(DentistsDbContext context)
    {
        _context = context;
    }

    public IDentistRepository Dentists
    {
        get
        {
            _dentistRepository ??= new DentistRepository(_context);
            return _dentistRepository;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // Azure SQL enables a retrying execution strategy, which forbids user-initiated
        // transactions unless the whole unit of work is wrapped in the strategy.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            var result = await operation(ct);

            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
    }
}
