using EVoting.Application.DTOs.Candidates;
using FluentValidation;

namespace EVoting.Application.Validators;

public class CreateCandidateRequestValidator : AbstractValidator<CreateCandidateRequestDto>
{
    public CreateCandidateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Party)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PhotoUrl)
            .MaximumLength(500);
    }
}
