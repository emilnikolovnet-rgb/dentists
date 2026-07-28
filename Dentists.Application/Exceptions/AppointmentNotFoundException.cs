namespace Dentists.Application.Exceptions;

public class AppointmentNotFoundException : BusinessException
{
    public AppointmentNotFoundException(Guid dentistId, Guid appointmentCorrelationId)
        : base(
            "Appointment not found",
            $"Dentist {dentistId} has no appointment with correlation id {appointmentCorrelationId}.")
    {
        DentistId = dentistId;
        AppointmentCorrelationId = appointmentCorrelationId;
    }

    public Guid DentistId { get; }

    public Guid AppointmentCorrelationId { get; }

    public override int StatusCode => 404;
}
