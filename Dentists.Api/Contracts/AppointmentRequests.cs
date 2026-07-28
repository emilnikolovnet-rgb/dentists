namespace Dentists.Api.Contracts;

using Dentists.Domain.Enums;

/// <summary>
/// Request bodies for the appointment endpoints. Separate from the commands so the dentist a
/// booking belongs to is taken from the route only, and cannot be contradicted by the body.
/// </summary>
public record AddAppointmentRequest(Guid AppointmentCorrelationId, DateTime ScheduledDate);

public record UpdateAppointmentRequest(DateTime ScheduledDate, Statuses Status);

public record SetAppointmentStatusRequest(Statuses Status);
