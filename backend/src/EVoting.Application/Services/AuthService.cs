using EVoting.Application.Common;
using EVoting.Application.DTOs.Admin;
using EVoting.Application.DTOs.Auth;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;

namespace EVoting.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            await _auditLogService.LogAsync(null, $"Registration failed: duplicate email ({request.Email})");
            await _unitOfWork.SaveChangesAsync();
            return Result<RegisterResponseDto>.Failure(AppError.DuplicateEmail, "A user with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Voter,
            IsVerified = true
        };

        await _userRepository.AddAsync(user);
        await _auditLogService.LogAsync(user.UserId, "User registered");
        await _unitOfWork.SaveChangesAsync();

        return Result<RegisterResponseDto>.Success(new RegisterResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email
        });
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _auditLogService.LogAsync(user?.UserId, $"Login failed: invalid credentials ({request.Email})");
            await _unitOfWork.SaveChangesAsync();
            return Result<LoginResponseDto>.Failure(AppError.InvalidCredentials, "Invalid email or password.");
        }

        var jwt = _jwtTokenService.GenerateToken(user.UserId, user.Role);
        await _auditLogService.LogAsync(user.UserId, "User logged in");
        await _unitOfWork.SaveChangesAsync();

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = jwt.Token,
            ExpiresAt = jwt.ExpiresAt,
            UserId = user.UserId,
            Role = user.Role.ToString()
        });
    }

    public async Task<Result<CreateUserResponseDto>> CreateUserAsync(CreateUserRequestDto request, Guid createdByUserId)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            await _auditLogService.LogAsync(createdByUserId, $"Admin user creation failed: duplicate email ({request.Email})");
            await _unitOfWork.SaveChangesAsync();
            return Result<CreateUserResponseDto>.Failure(AppError.DuplicateEmail, "A user with this email already exists.");
        }

        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsVerified = true
        };

        await _userRepository.AddAsync(user);
        await _auditLogService.LogAsync(createdByUserId, $"Admin created user {user.Email} with role {user.Role}");
        await _unitOfWork.SaveChangesAsync();

        return Result<CreateUserResponseDto>.Success(new CreateUserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }
}
