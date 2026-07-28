namespace Dentists.Domain.Entities;

public class Dentist
{
    public int Id { get; set; }

    /// <summary>
    /// Stable identifier other services use to reference this dentist.
    /// The database identity value is not usable for that: it is only unique per table
    /// and is not assigned until the row is inserted.
    /// </summary>
    public Guid CorrelationId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateTime LastUpdatedDate { get; private set; }

    public ICollection<DentistAppointment> Appointments { get; private set; } = new List<DentistAppointment>();

    // Constructor
    public Dentist() { }

    public Dentist(string firstName, string lastName)
    {
        CorrelationId = Guid.NewGuid();
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
}
