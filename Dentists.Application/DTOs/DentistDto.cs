namespace Dentists.Application.DTOs;

public class DentistDto
{
    /// <summary>
    /// The dentist's identifier, and what other services use to reference one. Since the move
    /// to Cosmos this is the document id and partition key rather than a store-assigned
    /// number, so it is stable across environments and restores.
    /// </summary>
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime LastUpdatedDate { get; set; }
}
