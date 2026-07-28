namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class CreateDentistCommandValidator : AbstractValidator<CreateDentistCommand>
{
    public CreateDentistCommandValidator()
    {
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
