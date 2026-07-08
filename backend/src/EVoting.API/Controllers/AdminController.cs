using System.Security.Claims;
using EVoting.Application.Common;
using EVoting.Application.DTOs.Admin;
using EVoting.Application.DTOs.Candidates;
using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using EVoting.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EVoting.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrator,ElectionOfficer")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IElectionService _electionService;
    private readonly ICandidateService _candidateService;
    private readonly IValidator<CreateUserRequestDto> _createUserValidator;
    private readonly IValidator<CreateElectionRequestDto> _createElectionValidator;
    private readonly IValidator<CreateCandidateRequestDto> _createCandidateValidator;
    private readonly IValidator<UpdateCandidateRequestDto> _updateCandidateValidator;

    public AdminController(
        IAuthService authService,
        IElectionService electionService,
        ICandidateService candidateService,
        IValidator<CreateUserRequestDto> createUserValidator,
        IValidator<CreateElectionRequestDto> createElectionValidator,
        IValidator<CreateCandidateRequestDto> createCandidateValidator,
        IValidator<UpdateCandidateRequestDto> updateCandidateValidator)
    {
        _authService = authService;
        _electionService = electionService;
        _candidateService = candidateService;
        _createUserValidator = createUserValidator;
        _createElectionValidator = createElectionValidator;
        _createCandidateValidator = createCandidateValidator;
        _updateCandidateValidator = updateCandidateValidator;
    }

    [HttpPost("users")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> CreateUser(CreateUserRequestDto request)
    {
        var validation = await _createUserValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _authService.CreateUserAsync(request, CurrentUserId());
        if (!result.Succeeded)
        {
            return MapError(result.Error, result.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("elections")]
    public async Task<IActionResult> CreateElection(CreateElectionRequestDto request)
    {
        var validation = await _createElectionValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _electionService.CreateElectionAsync(request, CurrentUserId());
        if (!result.Succeeded)
        {
            return MapError(result.Error, result.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("elections")]
    public async Task<IActionResult> ListElections()
    {
        var result = await _electionService.ListElectionsAsync();
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpGet("elections/{electionId:guid}/candidates")]
    public async Task<IActionResult> ListCandidates(Guid electionId)
    {
        var result = await _candidateService.ListCandidatesAsync(electionId);
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpPost("elections/{electionId:guid}/candidates")]
    public async Task<IActionResult> CreateCandidate(Guid electionId, CreateCandidateRequestDto request)
    {
        var validation = await _createCandidateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _candidateService.CreateCandidateAsync(electionId, request);
        if (!result.Succeeded)
        {
            return MapError(result.Error, result.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("elections/{electionId:guid}/candidates/{candidateId:guid}")]
    public async Task<IActionResult> UpdateCandidate(Guid electionId, Guid candidateId, UpdateCandidateRequestDto request)
    {
        var validation = await _updateCandidateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _candidateService.UpdateCandidateAsync(electionId, candidateId, request);
        return result.Succeeded ? Ok(result.Value) : MapError(result.Error, result.ErrorMessage);
    }

    [HttpDelete("elections/{electionId:guid}/candidates/{candidateId:guid}")]
    public async Task<IActionResult> DeleteCandidate(Guid electionId, Guid candidateId)
    {
        var result = await _candidateService.DeleteCandidateAsync(electionId, candidateId);
        return result.Succeeded ? NoContent() : MapError(result.Error, result.ErrorMessage);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static ModelStateDictionary BuildModelState(FluentValidation.Results.ValidationResult validation)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in validation.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return modelState;
    }

    private IActionResult MapError(AppError error, string? message)
    {
        return error switch
        {
            AppError.DuplicateEmail => Conflict(new { message }),
            AppError.NotFound => NotFound(new { message }),
            AppError.ElectionNotActive => Conflict(new { message }),
            _ => BadRequest(new { message })
        };
    }
}
