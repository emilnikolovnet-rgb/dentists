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
}
