namespace Dentists.Domain.Entities;

/// <summary>
/// An integration message waiting to be published, held inside the <see cref="Dentist"/> it was
/// raised from.
/// <para>
/// Embedding it is what makes the outbox work on Cosmos. The message shares the dentist's
/// partition key, so enqueuing it and the change that caused it are one write: a consumer
/// cannot commit a booking and then fail to record the message, nor announce something that
/// was never committed. A separate outbox store — in Cosmos or beside it — would be a second
/// transaction, which is the problem the pattern exists to remove.
/// </para>
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Carried onto the transport as the MessageId, so a redelivery is recognisable downstream.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Assembly-qualified-free type name used to rehydrate the payload for publishing.
    /// </summary>
    public string MessageType { get; private set; } = string.Empty;

    /// <summary>JSON body of the message.</summary>
    public string Payload { get; private set; } = string.Empty;

    public DateTime EnqueuedAt { get; private set; }

    /// <summary>When the dispatcher published it, or null while it is still pending.</summary>
    public DateTime? DispatchedAt { get; private set; }

    public bool IsDispatched => DispatchedAt.HasValue;

    // Constructor
    public OutboxMessage() { }

    internal OutboxMessage(Guid messageId, string messageType, string payload)
    {
        MessageId = messageId;
        MessageType = messageType;
        Payload = payload;
        EnqueuedAt = DateTime.UtcNow;
    }

    internal void MarkDispatched()
    {
        DispatchedAt ??= DateTime.UtcNow;
    }
}
