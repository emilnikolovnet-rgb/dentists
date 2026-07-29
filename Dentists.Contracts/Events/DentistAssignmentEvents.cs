namespace Dentists.Contracts.Events;

/// <summary>
/// A dentist has been found and the slot is held. Published from the dentist's own document
/// through the outbox, so it is only ever seen if the reservation really was committed.
/// </summary>
public record DentistReserved
{
    public Guid CorrelationId { get; init; }

    public Guid DentistId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public DateTime ScheduledDate { get; init; }

    public DateTime ReservedAt { get; init; }
}

/// <summary>
/// No dentist could be reserved. Drives the saga's compensation, which asks the Appointments
/// service to cancel the appointment.
/// </summary>
public record DentistReservationFailed
{
    public Guid CorrelationId { get; init; }

    public DateTime ScheduledDate { get; init; }

    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// A reserved dentist's hold has been given up and the slot is free again.
/// </summary>
public record DentistReleased
{
    public Guid CorrelationId { get; init; }

    public Guid DentistId { get; init; }

    public DateTime ReleasedAt { get; init; }
}

/// <summary>
/// The outcome of the assignment workflow, for anyone outside it: this appointment is now
/// backed by this dentist, in this state.
/// </summary>
public record DentistAppointmentStatusChanged
{
    public Guid CorrelationId { get; init; }

    public Guid DentistId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime ScheduledDate { get; init; }

    public DateTime ChangedAt { get; init; }
}
