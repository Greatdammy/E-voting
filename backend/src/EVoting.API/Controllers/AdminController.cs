using System.Security.Claims;
using EVoting.Application.Common;
using EVoting.Application.DTOs.Admin;
using EVoting.Application.Interfaces;
using EVoting.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EVoting.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<CreateUserRequestDto> _createUserValidator;

    public AdminController(IAuthService authService, IValidator<CreateUserRequestDto> createUserValidator)
    {
        _authService = authService;
        _createUserValidator = createUserValidator;
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequestDto request)
    {
        var validation = await _createUserValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        var createdByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _authService.CreateUserAsync(request, createdByUserId);
        if (!result.Succeeded)
        {
            return result.Error switch
            {
                AuthError.DuplicateEmail => Conflict(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
