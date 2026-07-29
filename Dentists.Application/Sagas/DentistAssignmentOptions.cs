namespace Dentists.Application.Sagas;

/// <summary>
/// Settings for the dentist-assignment workflow. Bound from the "DentistAssignment"
/// configuration section.
/// </summary>
public class DentistAssignmentOptions
{
    public const string SectionName = "DentistAssignment";

    /// <summary>
    /// How long a dentist is considered busy from the start of an appointment.
    /// <para>
    /// The Appointments service publishes only a start time, so the length of a booking is this
    /// service's assumption. It defines the window the availability search excludes and the one
    /// a conflict is judged against.
    /// </para>
    /// </summary>
    public TimeSpan AppointmentDuration { get; set; } = TimeSpan.FromHours(1);
}
