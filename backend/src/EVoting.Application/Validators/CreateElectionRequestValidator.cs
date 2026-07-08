using EVoting.Application.DTOs.Elections;
using FluentValidation;

namespace EVoting.Application.Validators;

public class CreateElectionRequestValidator : AbstractValidator<CreateElectionRequestDto>
{
    public CreateElectionRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.StartDate)
            .NotEmpty();

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.");
    }
}
