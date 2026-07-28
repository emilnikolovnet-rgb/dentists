namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class AddAppointmentCommandValidator : AbstractValidator<AddAppointmentCommand>
{
    public AddAppointmentCommandValidator()
    {
        RuleFor(x => x.DentistId)
            .NotEmpty();

        RuleFor(x => x.AppointmentCorrelationId)
            .NotEmpty();

        RuleFor(x => x.ScheduledDate)
            .NotEmpty();
    }
}
