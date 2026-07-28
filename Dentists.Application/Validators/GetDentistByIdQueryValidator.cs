namespace Dentists.Application.Validators;

using Dentists.Application.Queries;
using FluentValidation;

public class GetDentistByIdQueryValidator : AbstractValidator<GetDentistByIdQuery>
{
    public GetDentistByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
