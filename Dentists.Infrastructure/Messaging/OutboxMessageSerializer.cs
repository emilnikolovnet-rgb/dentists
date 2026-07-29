namespace Dentists.Infrastructure.Messaging;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Dentists.Application.Messaging;
using Dentists.Domain.Entities;

/// <summary>
/// Turns a contract into an outbox payload and back again.
/// <para>
/// A queued message may be published by a later build of this service, so the stored type name
/// has to survive a redeploy. It is the full type name without assembly or version, resolved
/// against the contracts assembly — the same principle MassTransit's own
/// <c>urn:message:Namespace:Type</c> naming follows, and for the same reason.
/// </para>
/// </summary>
public class OutboxMessageSerializer : IOutboxEnqueuer
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Assemblies a stored type name may name. Contracts today; more if that changes.</summary>
    private static readonly Assembly[] ContractAssemblies =
    [
        typeof(Dentists.Contracts.Events.DentistReserved).Assembly
    ];

    private static readonly ConcurrentDictionary<string, Type?> ResolvedTypes = new();

    public Guid Enqueue<TMessage>(Dentist dentist, TMessage message)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(dentist);
        ArgumentNullException.ThrowIfNull(message);

        var messageId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(message, PayloadOptions);

        dentist.EnqueueOutbox(messageId, TypeNameOf(typeof(TMessage)), payload);

        return messageId;
    }

    /// <summary>
    /// Rebuilds a queued message. Returns null when the stored type is no longer known, which
    /// happens if a contract is deleted or renamed while messages using it are still in flight.
    /// </summary>
    public static object? Deserialize(OutboxMessage message)
    {
        var type = ResolveType(message.MessageType);
        if (type is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize(message.Payload, type, PayloadOptions);
    }

    public static string TypeNameOf(Type type) => type.FullName!;

    private static Type? ResolveType(string typeName)
    {
        return ResolvedTypes.GetOrAdd(typeName, name =>
            ContractAssemblies
                .Select(assembly => assembly.GetType(name, throwOnError: false))
                .FirstOrDefault(t => t is not null)
            ?? Type.GetType(name, throwOnError: false));
    }
}
