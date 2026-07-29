// Mirrored from AppointmentsService/Appointments/Appointments.Application/Events.
// The namespace matches that service deliberately — MassTransit routes on namespace + type
// name, so changing it here stops the messages binding. See the project README.
namespace Appointments.Application.Events;

/// <summary>
/// Published once an appointment has been committed to the Appointments database. Starts the
/// dentist-assignment saga: it is the point at which a dentist has to be found and reserved.
/// </summary>
public record AppointmentCreated
{
    public Guid CorrelationId { get; init; }
    public int AppointmentId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime ScheduledDate { get; init; }
    public string? AdditionalInfo { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Published once changes to an appointment have been committed. Carries the new
/// <see cref="ScheduledDate"/>, so the saga treats it as a reschedule.
/// </summary>
public record AppointmentUpdated
{
    public Guid CorrelationId { get; init; }
    public int AppointmentId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime ScheduledDate { get; init; }
    public string? AdditionalInfo { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Published by the Appointments saga once an appointment has reached the confirmed state.
/// </summary>
public record AppointmentConfirmed
{
    public Guid CorrelationId { get; init; }
    public int AppointmentId { get; init; }
    public DateTime ScheduledDate { get; init; }
    public DateTime ConfirmedAt { get; init; }
}

/// <summary>
/// Published by the Appointments saga once an appointment has been cancelled.
/// </summary>
public record AppointmentCancelled
{
    public Guid CorrelationId { get; init; }
    public int AppointmentId { get; init; }
    public DateTime ScheduledDate { get; init; }
    public DateTime CancelledAt { get; init; }
    public string Reason { get; init; } = string.Empty;
}
