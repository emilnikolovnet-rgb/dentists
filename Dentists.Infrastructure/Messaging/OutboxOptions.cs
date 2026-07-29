namespace Dentists.Infrastructure.Messaging;

/// <summary>
/// Timings for the outbox dispatcher. Bound from the "Outbox" configuration section.
/// </summary>
public class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// How long to wait between sweeps for pending messages. Matches the QueryDelay the
    /// Appointments service gives MassTransit's own outbox.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How many dentists to drain per sweep. Caps the size of a single cross-partition query.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// How long a dispatched outbox message and a consumed inbox entry are kept before being
    /// pruned. Must comfortably exceed the transport's redelivery window, or a late redelivery
    /// would no longer be recognised as a duplicate. Matches Appointments' 30 minutes.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromMinutes(30);
}
