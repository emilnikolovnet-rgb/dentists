namespace Dentists.Application.Consumers;

using Dentists.Application.Messaging;
using Dentists.Application.Sagas;
using Dentists.Contracts.Events;
using Dentists.Contracts.Messages;
using Dentists.Domain.Entities;
using Dentists.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Finds a free dentist for an appointment and holds the slot.
/// <para>
/// The one place a dentist is chosen. The Appointments service publishes no dentist on any of
/// its events, so this decision — and the <see cref="DentistReserved"/> that records it — is
/// what connects an appointment to a person.
/// </para>
/// </summary>
public class ReserveDentistConsumer : IConsumer<ReserveDentist>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEnqueuer _outbox;
    private readonly IOptions<DentistAssignmentOptions> _options;
    private readonly ILogger<ReserveDentistConsumer> _logger;

    public ReserveDentistConsumer(
        IUnitOfWork unitOfWork,
        IOutboxEnqueuer outbox,
        IOptions<DentistAssignmentOptions> options,
        ILogger<ReserveDentistConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _options = options;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveDentist> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        // Idempotency for this consumer cannot use the inbox: on a redelivery there is no
        // dentist yet to have recorded it. Instead, ask whether anyone already holds this
        // booking — which is the state a first delivery would have left behind.
        var holder = await _unitOfWork.Dentists.FindByAppointmentCorrelationIdAsync(
            message.CorrelationId, cancellationToken);

        if (holder is not null)
        {
            _logger.LogInformation(
                "Appointment {CorrelationId} is already held by dentist {DentistId}; re-announcing",
                message.CorrelationId,
                holder.Id);

            await PublishReservedAsync(context, holder, message);
            return;
        }

        var from = message.ScheduledDate;
        var to = from + _options.Value.AppointmentDuration;

        var available = await _unitOfWork.Dentists.GetAvailableAsync(from, to, cancellationToken);
        var chosen = available.FirstOrDefault();

        if (chosen is null)
        {
            _logger.LogWarning(
                "No dentist available for appointment {AppointmentId} at {ScheduledDate}",
                message.AppointmentId,
                message.ScheduledDate);

            // Published directly rather than through the outbox, and correctly so: nothing was
            // written, so there is no database change for this message to be atomic with.
            await context.Publish(new DentistReservationFailed
            {
                CorrelationId = message.CorrelationId,
                ScheduledDate = message.ScheduledDate,
                Reason = $"No dentist is free at {message.ScheduledDate:u}."
            }, cancellationToken);

            return;
        }

        // GetAvailableAsync returns untracked dentists; reserve against a tracked one so the
        // booking and its outbox message are actually persisted.
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(chosen.Id, cancellationToken);
        if (dentist is null)
        {
            // Deleted between the availability query and this read.
            throw new InvalidOperationException(
                $"Dentist {chosen.Id} disappeared while reserving appointment {message.CorrelationId}.");
        }

        dentist.AddAppointment(message.CorrelationId, message.ScheduledDate);

        _outbox.Enqueue(dentist, new DentistReserved
        {
            CorrelationId = message.CorrelationId,
            DentistId = dentist.Id,
            FirstName = dentist.FirstName,
            LastName = dentist.LastName,
            ScheduledDate = message.ScheduledDate,
            ReservedAt = DateTime.UtcNow
        });

        // The booking and the announcement of it, in one Cosmos write.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reserved dentist {DentistId} for appointment {AppointmentId} at {ScheduledDate}",
            dentist.Id,
            message.AppointmentId,
            message.ScheduledDate);
    }

    private static Task PublishReservedAsync(
        ConsumeContext context,
        Dentist dentist,
        ReserveDentist message)
    {
        // A repeat announcement of a reservation that is already committed. Nothing is being
        // written, so this too goes straight out rather than through the outbox.
        return context.Publish(new DentistReserved
        {
            CorrelationId = message.CorrelationId,
            DentistId = dentist.Id,
            FirstName = dentist.FirstName,
            LastName = dentist.LastName,
            ScheduledDate = message.ScheduledDate,
            ReservedAt = DateTime.UtcNow
        }, context.CancellationToken);
    }
}
