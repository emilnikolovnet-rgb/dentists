using Dentists.Domain.Enums;

namespace Dentists.Domain.Entities;

/// <summary>
/// An appointment as the Dentists service sees it: which dentist is booked, when, and where
/// the booking has got to. The Appointments service remains the owner of the booking itself,
/// so this entity is keyed back to it by <see cref="AppointmentCorrelationId"/>.
/// </summary>
public class DentistAppointment
{

    public int Id { get; set; }

    /// <summary>
    /// The Appointments service's CorrelationId for this booking. Carried on its integration
    /// events, so it is what lets an incoming event find the row it belongs to.
    /// </summary>
    public Guid AppointmentCorrelationId { get; private set; }

    public int DentistId { get; private set; }

    public Dentist? Dentist { get; private set; }

    public DateTime ScheduledDate { get; private set; }

    public Statuses Status { get; private set; } = Statuses.Pending;

    public DateTime LastUpdatedDate { get; private set; }

    // Constructor
    public DentistAppointment() { }

    public DentistAppointment(Guid appointmentCorrelationId, int dentistId, DateTime scheduledDate)
    {
        AppointmentCorrelationId = appointmentCorrelationId;
        DentistId = dentistId;
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
