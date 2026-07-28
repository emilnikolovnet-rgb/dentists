namespace Dentists.Application.DTOs;

public class DentistDto
{
    public int Id { get; set; }

    /// <summary>
    /// Stable identifier other services use to reference this dentist.
    /// </summary>
    public Guid CorrelationId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime LastUpdatedDate { get; set; }
}
