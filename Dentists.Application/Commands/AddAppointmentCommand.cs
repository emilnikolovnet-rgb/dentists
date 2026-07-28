namespace Dentists.Application.Commands;

using Dentists.Application.DTOs;
using Dentists.Application.Exceptions;
using Dentists.Application.Mappings;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Records a booking the Appointments service has made against one of our dentists.
/// </summary>
public class AddAppointmentCommand : IRequest<DentistAppointmentDto>
{
    public Guid DentistId { get; set; }

    /// <summary>
    /// The Appointments service's identifier for the booking. Supplied by the caller rather
    /// than generated here: that service owns the booking, this one only mirrors it.
    /// </summary>
    public Guid AppointmentCorrelationId { get; set; }

    public DateTime ScheduledDate { get; set; }
}

public class AddAppointmentCommandHandler : IRequestHandler<AddAppointmentCommand, DentistAppointmentDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddAppointmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistAppointmentDto> Handle(AddAppointmentCommand request, CancellationToken cancellationToken)
    {
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.DentistId, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.DentistId);
        }

        // Idempotent: a redelivered event returns the booking already recorded rather than
        // adding a second copy or failing. The save below is then a no-op.
        var appointment = dentist.AddAppointment(request.AppointmentCorrelationId, request.ScheduledDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.ToDto(dentist.Id);
    }
}
