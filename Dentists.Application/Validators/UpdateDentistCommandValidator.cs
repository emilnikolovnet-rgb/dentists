namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class UpdateDentistCommandValidator : AbstractValidator<UpdateDentistCommand>
{
    public UpdateDentistCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        // Cosmos has no column widths, so the length cap the SQL model used to enforce now
        // only exists here.
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
