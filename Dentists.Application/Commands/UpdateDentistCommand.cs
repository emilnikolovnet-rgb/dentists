namespace Dentists.Application.Commands;

using Dentists.Application.DTOs;
using Dentists.Application.Exceptions;
using Dentists.Application.Mappings;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Renames a dentist. Appointments are managed through their own endpoints and are untouched.
/// </summary>
public class UpdateDentistCommand : IRequest<DentistDto>
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}

public class UpdateDentistCommandHandler : IRequestHandler<UpdateDentistCommand, DentistDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDentistCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistDto> Handle(UpdateDentistCommand request, CancellationToken cancellationToken)
    {
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.Id, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.Id);
        }

        dentist.Update(request.FirstName, request.LastName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dentist.ToDto();
    }
}
