using EVoting.Application.Common;
using EVoting.Application.DTOs.Auth;
using EVoting.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace EVoting.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.Succeeded)
        {
            return MapError(result.Error, result.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Succeeded)
        {
            return MapError(result.Error, result.ErrorMessage);
        }

        return Ok(result.Value);
    }

    private static ModelStateDictionary BuildModelState(ValidationResult validation)
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
            AppError.InvalidCredentials => Unauthorized(new { message }),
            _ => BadRequest(new { message })
        };
    }
}
