namespace Dentists.Domain.Entities;

/// <summary>
/// A record that this dentist has already had a given message applied to it.
/// <para>
/// Service Bus delivers at least once, so a consumer can be handed the same message twice.
/// Held inside the dentist for the same reason as <see cref="OutboxMessage"/>: the note that a
/// message was handled commits together with whatever handling it did, so the two can never
/// disagree.
/// </para>
/// <para>
/// MassTransit's own inbox would do this, but it is part of its Entity Framework outbox, which
/// supports SQL Server and Postgres only.
/// </para>
/// </summary>
public class InboxEntry
{
    public Guid MessageId { get; private set; }

    public DateTime ConsumedAt { get; private set; }

    // Constructor
    public InboxEntry() { }

    internal InboxEntry(Guid messageId)
    {
        MessageId = messageId;
        ConsumedAt = DateTime.UtcNow;
    }
}
