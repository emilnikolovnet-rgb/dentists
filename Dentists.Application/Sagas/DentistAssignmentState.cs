namespace Dentists.Application.Sagas;

using MassTransit;

/// <summary>
/// Persisted state for one appointment's dentist assignment.
/// <para>
/// Correlates on the Appointments service's CorrelationId, which is also what
/// <c>DentistAppointment.AppointmentCorrelationId</c> stores — one identity for the booking
/// across both services and the saga.
/// </para>
/// <para>
/// <see cref="DentistId"/> is the reason this saga exists. Nothing the Appointments service
/// publishes names a dentist, so when AppointmentConfirmed or AppointmentCancelled arrives
/// later this is the only record of which dentist is holding the slot. Without it, every such
/// event would need a cross-partition scan of all dentists to find out.
/// </para>
/// </summary>
public class DentistAssignmentState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = null!;

    /// <summary>The Appointments service's own integer key, needed to talk back to it.</summary>
    public int AppointmentId { get; set; }

    /// <summary>The dentist holding the slot, once one has been reserved.</summary>
    public Guid? DentistId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public DateTime? ReservedAt { get; set; }

    public string? FailureReason { get; set; }

    /// <summary>
    /// Set while a reschedule is being applied, so the reply can be told apart from the
    /// original reservation.
    /// </summary>
    public DateTime? PendingScheduledDate { get; set; }

    /// <summary>
    /// MassTransit's optimistic-concurrency token for the Cosmos saga repository.
    /// </summary>
    public string? ETag { get; set; }
}
