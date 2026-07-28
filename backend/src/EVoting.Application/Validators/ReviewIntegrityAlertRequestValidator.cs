using EVoting.Application.DTOs.Integrity;
using FluentValidation;

namespace EVoting.Application.Validators;

public class ReviewIntegrityAlertRequestValidator : AbstractValidator<ReviewIntegrityAlertRequestDto>
{
    public ReviewIntegrityAlertRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => status is "Reviewed" or "Dismissed")
            .WithMessage("Status must be either 'Reviewed' or 'Dismissed'.");

        RuleFor(x => x.Note)
            .MaximumLength(1000);
    }
}
