using System.Security.Claims;
using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EVoting.API.Controllers;

[ApiController]
[Route("api/elections")]
public class ElectionsController : ControllerBase
{
    private readonly IElectionService _electionService;
    private readonly IVoteService _voteService;
    private readonly IValidator<CastVoteRequestDto> _castVoteValidator;

    public ElectionsController(
        IElectionService electionService,
        IVoteService voteService,
        IValidator<CastVoteRequestDto> castVoteValidator)
    {
        _electionService = electionService;
        _voteService = voteService;
        _castVoteValidator = castVoteValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Voter")]
    public async Task<IActionResult> GetElections()
    {
        var result = await _electionService.GetElectionsForVoterAsync(CurrentUserId());
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpGet("{electionId:guid}/ballot")]
    [Authorize(Roles = "Voter")]
    public async Task<IActionResult> GetBallot(Guid electionId)
    {
        var result = await _electionService.GetBallotAsync(electionId, CurrentUserId());
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpPost("{electionId:guid}/vote")]
    [Authorize(Roles = "Voter")]
    public async Task<IActionResult> CastVote(Guid electionId, CastVoteRequestDto request)
    {
        var validation = await _castVoteValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        var result = await _voteService.CastVoteAsync(electionId, CurrentUserId(), request);
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpGet("{electionId:guid}/results")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(Guid electionId)
    {
        var result = await _electionService.GetResultsAsync(electionId);
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult MapError(AppError error, string? message)
    {
        return error switch
        {
            AppError.NotFound => NotFound(new { message }),
            AppError.ElectionNotActive => Conflict(new { message }),
            AppError.AlreadyVoted => Conflict(new { message }),
            AppError.InvalidCandidate => BadRequest(new { message }),
            _ => BadRequest(new { message })
        };
    }
}
