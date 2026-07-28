using System.Security.Claims;
using EVoting.Application.Common;
using EVoting.Application.DTOs.Integrity;
using EVoting.Application.Interfaces;
using EVoting.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EVoting.API.Controllers;

[ApiController]
[Route("api/admin/elections/{electionId:guid}")]
[Authorize(Roles = "Administrator,ElectionOfficer")]
public class IntegrityController : ControllerBase
{
    private readonly IIntegrityAlertService _integrityAlertService;
    private readonly IValidator<ReviewIntegrityAlertRequestDto> _reviewValidator;

    public IntegrityController(
        IIntegrityAlertService integrityAlertService,
        IValidator<ReviewIntegrityAlertRequestDto> reviewValidator)
    {
        _integrityAlertService = integrityAlertService;
        _reviewValidator = reviewValidator;
    }

    [HttpGet("integrity-alerts")]
    public async Task<IActionResult> ListAlerts(Guid electionId, [FromQuery] string? status)
    {
        IntegrityAlertStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (!Enum.TryParse<IntegrityAlertStatus>(status, ignoreCase: true, out var value))
            {
                return BadRequest(new { message = $"Unknown status '{status}'." });
            }

            parsedStatus = value;
        }

        var result = await _integrityAlertService.ListAlertsAsync(electionId, parsedStatus);
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpGet("integrity-summary")]
    public async Task<IActionResult> GetSummary(Guid electionId)
    {
        var result = await _integrityAlertService.GetSummaryAsync(electionId);
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpPost("integrity-alerts/{alertId:guid}/review")]
    public async Task<IActionResult> ReviewAlert(Guid electionId, Guid alertId, ReviewIntegrityAlertRequestDto request)
    {
        var validation = await _reviewValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        var result = await _integrityAlertService.ReviewAlertAsync(electionId, alertId, request, CurrentUserId());
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult MapError(AppError error, string? message)
    {
        return error switch
        {
            AppError.NotFound => NotFound(new { message }),
            _ => BadRequest(new { message })
        };
    }
}
