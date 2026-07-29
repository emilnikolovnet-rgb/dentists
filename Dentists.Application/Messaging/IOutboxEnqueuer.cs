namespace Dentists.Application.Messaging;

using Dentists.Domain.Entities;

/// <summary>
/// Queues an integration message on a dentist, taking care of serialising it and naming its
/// type in a way the dispatcher can reverse.
/// <para>
/// A seam rather than a static helper so consumers stay testable, and so the wire format is
/// decided in one place — the payloads written here have to be readable by whatever version of
/// the dispatcher picks them up later.
/// </para>
/// </summary>
public interface IOutboxEnqueuer
{
    /// <summary>
    /// Stages <paramref name="message"/> on <paramref name="dentist"/>. It is published only if
    /// the dentist is saved, and it is saved by the same Cosmos write as everything else the
    /// caller changed.
    /// </summary>
    /// <returns>The MessageId given to the queued message.</returns>
    Guid Enqueue<TMessage>(Dentist dentist, TMessage message)
        where TMessage : class;
}
