using EVoting.Application.DTOs.Elections;
using FluentValidation;

namespace EVoting.Application.Validators;

public class CastVoteRequestValidator : AbstractValidator<CastVoteRequestDto>
{
    public CastVoteRequestValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty();

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("Enter the verification code sent to your email.")
            .Matches(@"^\d{6}$").WithMessage("The verification code must be 6 digits.");
    }
}
