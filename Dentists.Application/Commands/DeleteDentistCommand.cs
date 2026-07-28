namespace Dentists.Application.Commands;

using Dentists.Application.Exceptions;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Deletes a dentist.
/// <para>
/// Soft: the document and the appointments embedded in it stay, and the dentist simply stops
/// being visible to every read. Bookings the Appointments service still holds are therefore
/// not destroyed, and the deletion can be undone by clearing the date.
/// </para>
/// </summary>
public class DeleteDentistCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteDentistCommandHandler : IRequestHandler<DeleteDentistCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDentistCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDentistCommand request, CancellationToken cancellationToken)
    {
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.Id, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.Id);
        }

        // The repository does not return soft-deleted dentists, so reaching this line means the
        // dentist was still active and a repeat delete correctly 404s above.
        dentist.MarkDeleted();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
