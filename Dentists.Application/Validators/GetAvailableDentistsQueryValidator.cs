namespace Dentists.Application.Validators;

using Dentists.Application.Queries;
using FluentValidation;

public class GetAvailableDentistsQueryValidator : AbstractValidator<GetAvailableDentistsQuery>
{
    public GetAvailableDentistsQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty();

        RuleFor(x => x.To)
            .NotEmpty()
            .GreaterThan(x => x.From)
            .WithMessage("'To' must be later than 'From'.");
    }
}
