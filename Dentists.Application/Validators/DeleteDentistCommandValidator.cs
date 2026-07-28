namespace Dentists.Application.Validators;

using Dentists.Application.Commands;
using FluentValidation;

public class DeleteDentistCommandValidator : AbstractValidator<DeleteDentistCommand>
{
    public DeleteDentistCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
