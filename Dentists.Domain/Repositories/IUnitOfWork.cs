namespace Dentists.Domain.Repositories;

public interface IUnitOfWork
{
    IDentistRepository Dentists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
