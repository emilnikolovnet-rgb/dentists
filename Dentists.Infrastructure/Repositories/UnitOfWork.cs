namespace Dentists.Infrastructure.Repositories;

using Dentists.Domain.Repositories;
using Dentists.Infrastructure.Persistence;

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
        // Cosmos has no user-initiated transaction to open: the provider rejects
        // BeginTransactionAsync outright. What it does give is a transactional batch per
        // partition key, which SaveChanges builds on its own. So the operation runs, and one
        // SaveChanges at the end commits every change it made to a given dentist atomically.
        // Changes spanning two dentists are two batches and can partially succeed.
        var result = await operation(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}
