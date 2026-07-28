namespace Dentists.Application.Exceptions;

public class DentistNotFoundException : BusinessException
{
    public DentistNotFoundException(int id)
        : base("Dentist not found", $"No dentist was found with id {id}.")
    {
        Id = id;
    }

    public int Id { get; }

    public override int StatusCode => 404;
}
