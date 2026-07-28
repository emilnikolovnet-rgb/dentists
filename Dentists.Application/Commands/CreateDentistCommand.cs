namespace Dentists.Application.Commands;

using Dentists.Application.DTOs;
using Dentists.Application.Mappings;
using Dentists.Domain.Entities;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Adds a dentist. Its identifier is minted by the aggregate — this service owns dentists, so
/// there is no external id to honour — and comes back on the result.
/// </summary>
public class CreateDentistCommand : IRequest<DentistDto>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}

public class CreateDentistCommandHandler : IRequestHandler<CreateDentistCommand, DentistDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateDentistCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistDto> Handle(CreateDentistCommand request, CancellationToken cancellationToken)
    {
        var dentist = new Dentist(request.FirstName, request.LastName);

        _unitOfWork.Dentists.Add(dentist);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dentist.ToDto();
    }
}
