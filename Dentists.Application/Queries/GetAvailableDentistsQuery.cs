namespace Dentists.Application.Queries;

using MediatR;
using Dentists.Application.DTOs;
using Dentists.Application.Mappings;

/// <summary>
/// Dentists with no live booking inside the requested window. The window is half-open,
/// [From, To), so a booking exactly at To does not make the dentist unavailable.
/// </summary>
public class GetAvailableDentistsQuery : IRequest<IEnumerable<DentistDto>>
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }
}

public class GetAvailableDentistsQueryHandler : IRequestHandler<GetAvailableDentistsQuery, IEnumerable<DentistDto>>
{
    private readonly Dentists.Domain.Repositories.IUnitOfWork _unitOfWork;

    public GetAvailableDentistsQueryHandler(Dentists.Domain.Repositories.IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DentistDto>> Handle(GetAvailableDentistsQuery request, CancellationToken cancellationToken)
    {
        var dentists = await _unitOfWork.Dentists.GetAvailableAsync(request.From, request.To, cancellationToken);

        return dentists.Select(d => d.ToDto()).ToList();
    }
}
