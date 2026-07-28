namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class DeleteAppointmentCommandValidator : AbstractValidator<DeleteAppointmentCommand>
{
    public DeleteAppointmentCommandValidator()
    {
        RuleFor(x => x.DentistId)
            .NotEmpty();

        RuleFor(x => x.AppointmentCorrelationId)
            .NotEmpty();
    }
}
