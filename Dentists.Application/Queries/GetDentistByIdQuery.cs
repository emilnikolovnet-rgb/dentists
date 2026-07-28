namespace Dentists.Application.Queries;

using MediatR;
using Dentists.Application.DTOs;
using Dentists.Application.Exceptions;
using Dentists.Application.Mappings;

public class GetDentistByIdQuery : IRequest<DentistDto>
{
    public Guid Id { get; set; }
}

public class GetDentistByIdQueryHandler : IRequestHandler<GetDentistByIdQuery, DentistDto>
{
    private readonly Dentists.Domain.Repositories.IUnitOfWork _unitOfWork;

    public GetDentistByIdQueryHandler(Dentists.Domain.Repositories.IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistDto> Handle(GetDentistByIdQuery request, CancellationToken cancellationToken)
    {
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.Id, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.Id);
        }

        return dentist.ToDto();
    }
}
