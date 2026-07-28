namespace Dentists.Application.Queries;

using MediatR;
using Dentists.Application.DTOs;
using Dentists.Application.Exceptions;
using Dentists.Application.Mappings;

public class GetAppointmentByIdQuery : IRequest<DentistAppointmentDto>
{
    public Guid DentistId { get; set; }

    public Guid AppointmentCorrelationId { get; set; }
}

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, DentistAppointmentDto>
{
    private readonly Dentists.Domain.Repositories.IUnitOfWork _unitOfWork;

    public GetAppointmentByIdQueryHandler(Dentists.Domain.Repositories.IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DentistAppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        // Reached through the dentist because that is the document the appointment lives in;
        // there is nothing to query it by on its own.
        var dentist = await _unitOfWork.Dentists.GetByIdAsync(request.DentistId, cancellationToken);
        if (dentist == null)
        {
            throw new DentistNotFoundException(request.DentistId);
        }

        var appointment = dentist.FindAppointment(request.AppointmentCorrelationId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(request.DentistId, request.AppointmentCorrelationId);
        }

        return appointment.ToDto(dentist.Id);
    }
}
