using Dentists.Domain.Enums;

namespace Dentists.Domain.Entities;

/// <summary>
/// An appointment as the Dentists service sees it: when the dentist is booked and where the
/// booking has got to. The Appointments service remains the owner of the booking itself, so
/// this entity is keyed back to it by <see cref="AppointmentCorrelationId"/>.
/// <para>
/// Embedded inside the owning <see cref="Dentist"/> document, so it carries no back reference
/// of its own — reach it through <see cref="Dentist.Appointments"/>.
/// </para>
/// </summary>
public class DentistAppointment
{
    /// <summary>
    /// The Appointments service's CorrelationId for this booking. Carried on its integration
    /// events, so it is what lets an incoming event find the entry it belongs to.
    /// </summary>
    public Guid AppointmentCorrelationId { get; private set; }

    public DateTime ScheduledDate { get; private set; }

    public Statuses Status { get; private set; } = Statuses.Pending;

    public DateTime LastUpdatedDate { get; private set; }

    // Constructor
    public DentistAppointment() { }

    internal DentistAppointment(Guid appointmentCorrelationId, DateTime scheduledDate)
    {
        AppointmentCorrelationId = appointmentCorrelationId;
        ScheduledDate = scheduledDate;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void Reschedule(DateTime scheduledDate)
    {
        ScheduledDate = scheduledDate;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies a status reported by the Appointments service. Cancelled is terminal there,
    /// so it is not moved off here either.
    /// </summary>
    public void SetStatus(Statuses status)
    {
        if (Status == Statuses.Cancelled)
        {
            throw new InvalidOperationException(
                $"Appointment {AppointmentCorrelationId} is cancelled and cannot become {status}.");
        }

        Status = status;
        LastUpdatedDate = DateTime.UtcNow;
    }
}
