// Mirrored from AppointmentsService/Appointments/Appointments.Application/Messages.
// The namespace matches that service deliberately — see the project README.
namespace Appointments.Application.Messages;

/// <summary>
/// Asks the Appointments service to confirm an appointment. Its public inbound contract,
/// consumed there by ConfirmAppointmentRequestedConsumer.
/// <para>
/// Sent once a dentist has been reserved: from this service's point of view the appointment
/// is now backed by a real person and can proceed.
/// </para>
/// </summary>
public record ConfirmAppointmentRequested
{
    public int AppointmentId { get; init; }

    /// <summary>Optional free-text origin of the request, for observability only.</summary>
    public string? Source { get; init; }
}

/// <summary>
/// Asks the Appointments service to cancel an appointment. Used to compensate when no dentist
/// can be reserved.
/// <para>
/// Unlike <see cref="ConfirmAppointmentRequested"/> this is an internal command of the
/// Appointments saga rather than a contract it advertises for outside use. It is sent to
/// <c>queue:cancel-appointment</c>, which its CancelAppointmentConsumer already serves.
/// Replacing this with a public CancelAppointmentRequested is the tidier long-term shape and
/// needs a change on that side.
/// </para>
/// </summary>
public record CancelAppointment
{
    public Guid CorrelationId { get; init; }
    public int AppointmentId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
