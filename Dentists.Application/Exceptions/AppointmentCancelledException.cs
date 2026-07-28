namespace Dentists.Application.Exceptions;

/// <summary>
/// Cancelled is terminal in the Appointments service, so it is terminal here too. Reported as
/// a conflict rather than a bad request: the same call would have succeeded before the booking
/// was cancelled, so what is wrong is the state, not the request.
/// </summary>
public class AppointmentCancelledException : BusinessException
{
    public AppointmentCancelledException(Guid appointmentCorrelationId)
        : base(
            "Appointment is cancelled",
            $"Appointment {appointmentCorrelationId} is cancelled and can no longer be updated.")
    {
        AppointmentCorrelationId = appointmentCorrelationId;
    }

    public Guid AppointmentCorrelationId { get; }

    public override int StatusCode => 409;
}
