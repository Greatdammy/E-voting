using EVoting.Application.DTOs.Candidates;
using FluentValidation;

namespace EVoting.Application.Validators;

public class UpdateCandidateRequestValidator : AbstractValidator<UpdateCandidateRequestDto>
{
    public UpdateCandidateRequestValidator()
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
