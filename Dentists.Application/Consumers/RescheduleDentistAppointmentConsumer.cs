namespace Dentists.Application.Consumers;

using Dentists.Application.Messaging;
using Dentists.Application.Sagas;
using Dentists.Contracts.Events;
using Dentists.Contracts.Messages;
using Dentists.Domain.Enums;
using Dentists.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Moves a booking to a new time on the dentist already holding it.
/// </summary>
public class RescheduleDentistAppointmentConsumer : IConsumer<RescheduleDentistAppointment>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEnqueuer _outbox;
    private readonly IOptions<DentistAssignmentOptions> _options;
    private readonly ILogger<RescheduleDentistAppointmentConsumer> _logger;

    public RescheduleDentistAppointmentConsumer(
        IUnitOfWork unitOfWork,
        IOutboxEnqueuer outbox,
        IOptions<DentistAssignmentOptions> options,
        ILogger<RescheduleDentistAppointmentConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _options = options;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RescheduleDentistAppointment> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var dentist = await _unitOfWork.Dentists.GetByIdAsync(message.DentistId, cancellationToken);
        if (dentist is null)
        {
            _logger.LogWarning(
                "Dentist {DentistId} is gone; dropping reschedule of {CorrelationId}",
                message.DentistId,
                message.CorrelationId);
            return;
        }

        var messageId = context.MessageId ?? Guid.Empty;
        if (messageId != Guid.Empty && dentist.HasConsumed(messageId))
        {
            _logger.LogInformation(
                "Message {MessageId} already applied to dentist {DentistId}; ignoring redelivery",
                messageId,
                dentist.Id);
            return;
        }

        var appointment = dentist.FindAppointment(message.CorrelationId);
        if (appointment is null)
        {
            _logger.LogWarning(
                "Dentist {DentistId} has no booking {CorrelationId}; dropping reschedule",
                dentist.Id,
                message.CorrelationId);
            return;
        }

        if (appointment.Status == Statuses.Cancelled)
        {
            _logger.LogWarning(
                "Booking {CorrelationId} is cancelled; refusing to reschedule it",
                message.CorrelationId);
            return;
        }

        // The booking keeps its dentist. If that dentist is now double-booked at the new time
        // the move still goes ahead, because the Appointments service has already committed to
        // it and refusing here would leave the two services disagreeing about when it is.
        // Reassigning to a different dentist instead is the open piece of this workflow; until
        // then the collision is surfaced rather than silently absorbed.
        var to = message.ScheduledDate + _options.Value.AppointmentDuration;
        if (dentist.HasConflict(message.ScheduledDate, to, exceptAppointment: message.CorrelationId))
        {
            _logger.LogWarning(
                "Rescheduling {CorrelationId} to {ScheduledDate} double-books dentist {DentistId}; " +
                "applied anyway, needs manual reassignment",
                message.CorrelationId,
                message.ScheduledDate,
                dentist.Id);
        }

        dentist.RescheduleAppointment(message.CorrelationId, message.ScheduledDate);

        _outbox.Enqueue(dentist, new DentistAppointmentStatusChanged
        {
            CorrelationId = message.CorrelationId,
            DentistId = dentist.Id,
            Status = appointment.Status.ToString(),
            ScheduledDate = message.ScheduledDate,
            ChangedAt = DateTime.UtcNow
        });

        if (messageId != Guid.Empty)
        {
            dentist.RecordConsumed(messageId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {CorrelationId} on dentist {DentistId} moved to {ScheduledDate}",
            message.CorrelationId,
            dentist.Id,
            message.ScheduledDate);
    }
}
