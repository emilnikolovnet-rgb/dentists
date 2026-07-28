namespace Dentists.Api.Contracts;

/// <summary>
/// Request bodies for the dentist endpoints. Separate from the commands so the dentist being
/// updated is taken from the route only, and cannot be contradicted by the body.
/// </summary>
public record CreateDentistRequest(string FirstName, string LastName);

public record UpdateDentistRequest(string FirstName, string LastName);
