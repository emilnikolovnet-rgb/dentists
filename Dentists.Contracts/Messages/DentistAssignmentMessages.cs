// Commands of the dentist-assignment workflow. Sent by the saga to a known endpoint rather
// than published, because exactly one consumer should act on each.
//
// The saga itself never touches the database. It sends these instead, so that every change to
// a dentist happens inside a consumer, where the business change, the inbox entry and any
// outbox messages commit as a single Cosmos write.
namespace Dentists.Contracts.Messages;

/// <summary>
/// Find a free dentist for this appointment and hold the slot.
/// </summary>
public record ReserveDentist
{
    /// <summary>The Appointments service's CorrelationId. Identifies the saga and the booking.</summary>
    public Guid CorrelationId { get; init; }

    public int AppointmentId { get; init; }

    public DateTime ScheduledDate { get; init; }
}

/// <summary>
/// Give up a dentist's hold on an appointment, removing the booking from that dentist.
/// </summary>
public record ReleaseDentist
{
    public Guid CorrelationId { get; init; }

    public Guid DentistId { get; init; }
}

/// <summary>
/// Apply a status the Appointments service has reported to the reserved dentist's copy.
/// </summary>
public record SetDentistAppointmentStatus
{
    public Guid CorrelationId { get; init; }

    public Guid DentistId { get; init; }

    /// <summary>Name of a <c>Dentists.Domain.Enums.Statuses</c> member.</summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Move a booking to a new time on the dentist already holding it.
/// </summary>
public record RescheduleDentistAppointment
{
    public Guid CorrelationId { get; init; }

    public Guid DentistId { get; init; }

    public DateTime ScheduledDate { get; init; }
}
