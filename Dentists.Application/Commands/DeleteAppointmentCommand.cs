namespace Dentists.Application.Commands;

using Dentists.Application.Exceptions;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Drops a booking from a dentist entirely.
/// <para>
/// Distinct from cancelling it: a cancelled booking stays on the dentist with a terminal
/// status and remains visible, whereas this removes the record. Reserve it for bookings that
/// should never have reached this service.
/// </para>
/// </summary>
public class DeleteAppointmentCommand : IRequest
{
    public Guid DentistId { get; set; }

    public Guid AppointmentCorrelationId { get; set; }
}

public class DeleteAppointmentCommandHandler : IRequestHandler<DeleteAppointmentCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAppointmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.DentistId, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.DentistId);
        }

        if (!dentist.RemoveAppointment(request.AppointmentCorrelationId))
        {
            throw new AppointmentNotFoundException(request.DentistId, request.AppointmentCorrelationId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
