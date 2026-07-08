using EVoting.Application.DTOs.Elections;
using FluentValidation;

namespace EVoting.Application.Validators;

public class CastVoteRequestValidator : AbstractValidator<CastVoteRequestDto>
{
    public CastVoteRequestValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty();
    }
}
