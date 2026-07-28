namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class SetAppointmentStatusCommandValidator : AbstractValidator<SetAppointmentStatusCommand>
{
    public SetAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.DentistId)
            .NotEmpty();

        RuleFor(x => x.AppointmentCorrelationId)
            .NotEmpty();

        // Guards against a value outside the enum arriving as a number and being stored as one.
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
