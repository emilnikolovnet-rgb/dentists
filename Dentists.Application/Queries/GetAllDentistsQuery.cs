namespace Dentists.Application.Queries;

using MediatR;
using Dentists.Application.DTOs;
using Dentists.Application.Mappings;

public class GetAllDentistsQuery : IRequest<IEnumerable<DentistDto>>
{
}

public class GetAllDentistsQueryHandler : IRequestHandler<GetAllDentistsQuery, IEnumerable<DentistDto>>
{
    private readonly Dentists.Domain.Repositories.IUnitOfWork _unitOfWork;

    public GetAllDentistsQueryHandler(Dentists.Domain.Repositories.IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DentistDto>> Handle(GetAllDentistsQuery request, CancellationToken cancellationToken)
    {
        var dentists = await _unitOfWork.Dentists.GetAllAsync(cancellationToken);

        return dentists.Select(d => d.ToDto()).ToList();
    }
}
