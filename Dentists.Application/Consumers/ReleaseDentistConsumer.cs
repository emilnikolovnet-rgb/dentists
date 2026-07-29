namespace Dentists.Application.Consumers;

using Dentists.Application.Messaging;
using Dentists.Contracts.Events;
using Dentists.Contracts.Messages;
using Dentists.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

/// <summary>
/// Gives up a dentist's hold on an appointment, removing the booking outright.
/// <para>
/// Distinct from cancelling it: a cancelled booking stays on the dentist with a terminal
/// status and remains visible. This is for a hold that should never have been taken — the
/// slot simply becomes free again.
/// </para>
/// </summary>
public class ReleaseDentistConsumer : IConsumer<ReleaseDentist>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEnqueuer _outbox;
    private readonly ILogger<ReleaseDentistConsumer> _logger;

    public ReleaseDentistConsumer(
        IUnitOfWork unitOfWork,
        IOutboxEnqueuer outbox,
        ILogger<ReleaseDentistConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseDentist> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var dentist = await _unitOfWork.Dentists.GetByIdAsync(message.DentistId, cancellationToken);
        if (dentist is null)
        {
            _logger.LogWarning(
                "Dentist {DentistId} is gone; nothing to release for {CorrelationId}",
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

        if (!dentist.RemoveAppointment(message.CorrelationId))
        {
            // Already released, by an earlier delivery or by hand. The end state is the one
            // asked for, so this is a success rather than something to retry.
            _logger.LogInformation(
                "Dentist {DentistId} was not holding {CorrelationId}; nothing to do",
                dentist.Id,
                message.CorrelationId);
            return;
        }

        _outbox.Enqueue(dentist, new DentistReleased
        {
            CorrelationId = message.CorrelationId,
            DentistId = dentist.Id,
            ReleasedAt = DateTime.UtcNow
        });

        if (messageId != Guid.Empty)
        {
            dentist.RecordConsumed(messageId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Released dentist {DentistId} from booking {CorrelationId}",
            dentist.Id,
            message.CorrelationId);
    }
}
