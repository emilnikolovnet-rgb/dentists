namespace Dentists.Application.Validators;

using Dentists.Application.Queries;
using FluentValidation;

public class GetAppointmentByIdQueryValidator : AbstractValidator<GetAppointmentByIdQuery>
{
    public GetAppointmentByIdQueryValidator()
    {
        RuleFor(x => x.DentistId)
            .NotEmpty();

        RuleFor(x => x.AppointmentCorrelationId)
            .NotEmpty();
    }
}
