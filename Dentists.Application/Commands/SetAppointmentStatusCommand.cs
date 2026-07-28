namespace Dentists.Application.Commands;

using Dentists.Application.DTOs;
using Dentists.Application.Exceptions;
using Dentists.Application.Mappings;
using Dentists.Domain.Enums;
using Dentists.Domain.Repositories;
using MediatR;

/// <summary>
/// Moves a booking to a new status, leaving its scheduled date alone.
/// <para>
/// Separate from <see cref="UpdateAppointmentCommand"/> because a status change is what the
/// Appointments service reports most often, and requiring the caller to resend a scheduled
/// date it is not changing invites it to send a stale one.
/// </para>
/// </summary>
public class SetAppointmentStatusCommand : IRequest<DentistAppointmentDto>
{
    public Guid DentistId { get; set; }

    public Guid AppointmentCorrelationId { get; set; }

    public Statuses Status { get; set; }
}

public class SetAppointmentStatusCommandHandler : IRequestHandler<SetAppointmentStatusCommand, DentistAppointmentDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SetAppointmentStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistAppointmentDto> Handle(SetAppointmentStatusCommand request, CancellationToken cancellationToken)
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

        // Cancelled is terminal, so only a repeat of the cancellation itself gets through.
        // Checked here so the caller sees a 409 rather than the InvalidOperationException the
        // aggregate would raise, which the handler could only report as a 500.
        if (existing.Status == Statuses.Cancelled && request.Status != Statuses.Cancelled)
        {
            throw new AppointmentCancelledException(request.AppointmentCorrelationId);
        }

        var appointment = dentist.SetAppointmentStatus(request.AppointmentCorrelationId, request.Status)!;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.ToDto(dentist.Id);
    }
}
