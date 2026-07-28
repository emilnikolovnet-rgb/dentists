namespace Dentists.Application.Commands;

using Dentists.Application.DTOs;
using Dentists.Application.Exceptions;
using Dentists.Application.Mappings;
using Dentists.Domain.Enums;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Applies the Appointments service's current view of a booking: when it is scheduled and
/// where it has got to.
/// </summary>
public class UpdateAppointmentCommand : IRequest<DentistAppointmentDto>
{
    public Guid DentistId { get; set; }

    public Guid AppointmentCorrelationId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public Statuses Status { get; set; }
}

public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, DentistAppointmentDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAppointmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistAppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.DentistId, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.DentistId);
        }

        var existing = dentist.FindAppointment(request.AppointmentCorrelationId);
        if (existing == null)
        {
            throw new AppointmentNotFoundException(request.DentistId, request.AppointmentCorrelationId);
        }

        // Checked here so the caller gets a 409 rather than the InvalidOperationException the
        // aggregate would otherwise raise, which the handler could only report as a 500.
        if (existing.Status == Statuses.Cancelled)
        {
            throw new AppointmentCancelledException(request.AppointmentCorrelationId);
        }

        var appointment = dentist.UpdateAppointment(
            request.AppointmentCorrelationId,
            request.ScheduledDate,
            request.Status)!;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.ToDto(dentist.Id);
    }
}
