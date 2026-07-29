namespace Dentists.Application.Consumers;

using Dentists.Application.Messaging;
using Dentists.Contracts.Events;
using Dentists.Contracts.Messages;
using Dentists.Domain.Enums;
using Dentists.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

/// <summary>
/// Applies a status the Appointments service has reported to the reserved dentist's copy of
/// the booking.
/// </summary>
public class SetDentistAppointmentStatusConsumer : IConsumer<SetDentistAppointmentStatus>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEnqueuer _outbox;
    private readonly ILogger<SetDentistAppointmentStatusConsumer> _logger;

    public SetDentistAppointmentStatusConsumer(
        IUnitOfWork unitOfWork,
        IOutboxEnqueuer outbox,
        ILogger<SetDentistAppointmentStatusConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SetDentistAppointmentStatus> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        if (!Enum.TryParse<Statuses>(message.Status, ignoreCase: false, out var status))
        {
            // Not retryable: redelivering the same unparseable value cannot help.
            throw new ArgumentOutOfRangeException(
                nameof(message.Status),
                message.Status,
                $"Not a member of {nameof(Statuses)}.");
        }

        var dentist = await _unitOfWork.Dentists.GetByIdAsync(message.DentistId, cancellationToken);
        if (dentist is null)
        {
            _logger.LogWarning(
                "Dentist {DentistId} is gone; dropping status {Status} for appointment {CorrelationId}",
                message.DentistId,
                message.Status,
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
                "Dentist {DentistId} has no booking {CorrelationId}; dropping status {Status}",
                dentist.Id,
                message.CorrelationId,
                message.Status);
            return;
        }

        // Cancelled is terminal in the domain, so anything other than a repeat of it would
        // throw. A late Confirmed after a cancellation is a reordering, not a fault.
        if (appointment.Status == Statuses.Cancelled && status != Statuses.Cancelled)
        {
            _logger.LogWarning(
                "Booking {CorrelationId} is cancelled; refusing to move it to {Status}",
                message.CorrelationId,
                status);
            return;
        }

        dentist.SetAppointmentStatus(message.CorrelationId, status);

        _outbox.Enqueue(dentist, new DentistAppointmentStatusChanged
        {
            CorrelationId = message.CorrelationId,
            DentistId = dentist.Id,
            Status = status.ToString(),
            ScheduledDate = appointment.ScheduledDate,
            ChangedAt = DateTime.UtcNow
        });

        if (messageId != Guid.Empty)
        {
            dentist.RecordConsumed(messageId);
        }

        // Status change, inbox entry and outbox message: one write.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {CorrelationId} on dentist {DentistId} is now {Status}",
            message.CorrelationId,
            dentist.Id,
            status);
    }
}
