namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator()
    {
        RuleFor(x => x.DentistId)
            .NotEmpty();

        RuleFor(x => x.AppointmentCorrelationId)
            .NotEmpty();

        RuleFor(x => x.ScheduledDate)
            .NotEmpty();

        // Guards against a value outside the enum arriving as a number and being stored as one.
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
