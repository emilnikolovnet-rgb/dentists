namespace Dentists.Application.Mappings;

using Dentists.Application.DTOs;
using Dentists.Domain.Entities;

/// <summary>
/// Single place where a dentist becomes a DTO, so the read endpoints cannot drift apart.
/// </summary>
public static class DentistMappings
{
    public static DentistDto ToDto(this Dentist dentist)
    {
        return new DentistDto
        {
            Id = dentist.Id,
            FirstName = dentist.FirstName,
            LastName = dentist.LastName,
            LastUpdatedDate = dentist.LastUpdatedDate
        };
    }
}
