namespace Dentists.Infrastructure.Messaging;

using Dentists.Domain.Entities;
using Dentists.Domain.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Publishes the integration messages that consumers queued inside dentist documents.
/// <para>
/// The delivery half of the transactional outbox. Consumers only ever write to the aggregate;
/// nothing they do reaches the transport until this drains it, which is what lets a business
/// change and its announcement commit as one Cosmos write.
/// </para>
/// <para>
/// Delivery is at least once by design. A message is published first and marked dispatched
/// afterwards, so a crash in between republishes it rather than losing it — the safe direction
/// to fail, and the reason consumers keep an inbox.
/// </para>
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = _options.Value;

        _logger.LogInformation(
            "Outbox dispatcher started, sweeping every {PollInterval}", settings.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(settings, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Never let one bad sweep end the loop: the messages are durable, so the worst
                // outcome of a failure here is that they go out on a later pass.
                _logger.LogError(exception, "Outbox sweep failed; retrying next interval");
            }

            try
            {
                await Task.Delay(settings.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox dispatcher stopped");
    }

    private async Task SweepAsync(OutboxOptions settings, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var dentists = await unitOfWork.Dentists.GetWithPendingOutboxAsync(
            settings.BatchSize, cancellationToken);

        if (dentists.Count == 0)
        {
            return;
        }

        var cutoff = DateTime.UtcNow - settings.RetentionPeriod;
        var published = 0;

        foreach (var dentist in dentists)
        {
            published += await DrainAsync(dentist, publishEndpoint, cancellationToken);
            dentist.PruneMessages(cutoff);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // A consumer changed one of these dentists while we were publishing. The messages
            // went out; the marks did not stick. They will be republished next sweep and the
            // receiving inbox will discard the repeat.
            _logger.LogWarning(
                exception,
                "Outbox marks lost to a concurrent write; {Count} message(s) will be republished",
                published);
            return;
        }

        if (published > 0)
        {
            _logger.LogInformation(
                "Dispatched {Count} outbox message(s) across {DentistCount} dentist(s)",
                published,
                dentists.Count);
        }
    }

    private async Task<int> DrainAsync(
        Dentist dentist,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var published = 0;

        foreach (var message in dentist.PendingOutbox().ToList())
        {
            var contract = OutboxMessageSerializer.Deserialize(message);
            if (contract is null)
            {
                // The contract has been renamed or removed since this was queued. Publishing is
                // impossible, and leaving it pending would make it block the log forever, so it
                // is marked done and reported loudly.
                _logger.LogError(
                    "Outbox message {MessageId} on dentist {DentistId} names unknown type {MessageType}; discarding",
                    message.MessageId,
                    dentist.Id,
                    message.MessageType);

                dentist.MarkDispatched(message.MessageId);
                continue;
            }

            // Carrying the stored id onto the transport is what makes a republish recognisable
            // as the same message rather than a new one.
            await publishEndpoint.Publish(
                contract,
                contract.GetType(),
                Pipe.Execute<PublishContext>(context => context.MessageId = message.MessageId),
                cancellationToken);

            dentist.MarkDispatched(message.MessageId);
            published++;
        }

        return published;
    }
}
