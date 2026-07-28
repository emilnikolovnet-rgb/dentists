using Newtonsoft.Json;

namespace Dentists.Domain.Entities;

public class Dentist
{
    /// <summary>
    /// The dentist's identity, and the identifier other services use to reference one.
    /// Doubles as the document id and partition key in Cosmos, so it is assigned by the
    /// application at construction and never by the store.
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateTime LastUpdatedDate { get; private set; }

    /// <summary>
    /// Embedded in the dentist document rather than stored separately, so availability is
    /// answerable from one document and the aggregate is written atomically.
    /// </summary>
    public ICollection<DentistAppointment> Appointments { get; private set; } = new List<DentistAppointment>();

    // Constructor
    public Dentist() { }

    public Dentist(string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void Update(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a booking against this dentist. One entry per booking: an event redelivered by
    /// the Appointments service must not create a second copy. Cosmos has no unique index to
    /// enforce that, so the aggregate does it — which is safe because the whole collection
    /// lives in this document and is written under a single etag check.
    /// </summary>
    public DentistAppointment AddAppointment(Guid appointmentCorrelationId, DateTime scheduledDate)
    {
        var existing = FindAppointment(appointmentCorrelationId);
        if (existing is not null)
        {
            return existing;
        }

        var appointment = new DentistAppointment(appointmentCorrelationId, scheduledDate);
        Appointments.Add(appointment);
        LastUpdatedDate = DateTime.UtcNow;

        return appointment;
    }

    public DentistAppointment? FindAppointment(Guid appointmentCorrelationId)
    {
        return Appointments.FirstOrDefault(a => a.AppointmentCorrelationId == appointmentCorrelationId);
    }
}
