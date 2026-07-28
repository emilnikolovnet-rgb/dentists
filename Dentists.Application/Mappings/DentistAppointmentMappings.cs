namespace Dentists.Application.Mappings;

using Dentists.Application.DTOs;
using Dentists.Domain.Entities;

/// <summary>
/// Single place where an appointment becomes a DTO, so the write endpoints cannot drift apart.
/// </summary>
public static class DentistAppointmentMappings
{
    /// <param name="dentistId">
    /// Supplied by the caller because an embedded appointment carries no reference back to the
    /// dentist it belongs to.
    /// </param>
    public static DentistAppointmentDto ToDto(this DentistAppointment appointment, Guid dentistId)
    {
        return new DentistAppointmentDto
        {
            AppointmentCorrelationId = appointment.AppointmentCorrelationId,
            DentistId = dentistId,
            ScheduledDate = appointment.ScheduledDate,
            Status = appointment.Status,
            LastUpdatedDate = appointment.LastUpdatedDate
        };
    }
}
