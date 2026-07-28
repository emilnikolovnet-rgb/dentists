namespace Dentists.Application.DTOs;

using Dentists.Domain.Enums;

public class DentistAppointmentDto
{
    /// <summary>
    /// The Appointments service's identifier for this booking. It identifies the booking
    /// everywhere, including on the routes of this service.
    /// </summary>
    public Guid AppointmentCorrelationId { get; set; }

    /// <summary>
    /// The dentist the booking belongs to. Not stored on the appointment itself — it is the
    /// document the appointment is embedded in — so it is filled in from the owning aggregate.
    /// </summary>
    public Guid DentistId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public Statuses Status { get; set; }

    public DateTime LastUpdatedDate { get; set; }
}
