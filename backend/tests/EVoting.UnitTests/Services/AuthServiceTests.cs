using EVoting.Application.Common;
using EVoting.Application.DTOs.Admin;
using EVoting.Application.DTOs.Auth;
using EVoting.Application.Interfaces;
using EVoting.Application.Services;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Moq;
using Xunit;

namespace EVoting.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            _auditLogService.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task RegisterAsync_CreatesVoter_WhenEmailIsNew()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("new@example.com")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash("Password1")).Returns("hashed");

        var request = new RegisterRequestDto { FullName = "Jane Doe", Email = "new@example.com", Password = "Password1" };

        var result = await _sut.RegisterAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal("new@example.com", result.Value!.Email);
        _userRepository.Verify(
            r => r.AddAsync(It.Is<User>(u => u.Role == UserRole.Voter && u.PasswordHash == "hashed")),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsDuplicateEmail_WhenEmailExists()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(new User { Email = "existing@example.com" });

        var request = new RegisterRequestDto { FullName = "Jane Doe", Email = "existing@example.com", Password = "Password1" };

        var result = await _sut.RegisterAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.DuplicateEmail, result.Error);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        var user = new User { Email = "voter@example.com", PasswordHash = "hashed", Role = UserRole.Voter };
        _userRepository.Setup(r => r.GetByEmailAsync("voter@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Password1", "hashed")).Returns(true);
        _jwtTokenService
            .Setup(j => j.GenerateToken(user.UserId, UserRole.Voter))
            .Returns(new JwtResult("token123", DateTime.UtcNow.AddHours(8)));

        var request = new LoginRequestDto { Email = "voter@example.com", Password = "Password1" };

        var result = await _sut.LoginAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal("token123", result.Value!.Token);
    }

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenEmailNotFound()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("missing@example.com")).ReturnsAsync((User?)null);

        var request = new LoginRequestDto { Email = "missing@example.com", Password = "whatever" };

        var result = await _sut.LoginAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenPasswordIsWrong()
    {
        var user = new User { Email = "voter@example.com", PasswordHash = "hashed", Role = UserRole.Voter };
        _userRepository.Setup(r => r.GetByEmailAsync("voter@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("WrongPassword", "hashed")).Returns(false);

        var request = new LoginRequestDto { Email = "voter@example.com", Password = "WrongPassword" };

        var result = await _sut.LoginAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUserWithSpecifiedRole()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("officer@example.com")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash("Password1")).Returns("hashed");

        var request = new CreateUserRequestDto
        {
            FullName = "New Officer",
            Email = "officer@example.com",
            Password = "Password1",
            Role = "ElectionOfficer"
        };

        var result = await _sut.CreateUserAsync(request, Guid.NewGuid());

        Assert.True(result.Succeeded);
        Assert.Equal("ElectionOfficer", result.Value!.Role);
        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.Role == UserRole.ElectionOfficer)), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsDuplicateEmail_WhenEmailExists()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(new User { Email = "existing@example.com" });

        var request = new CreateUserRequestDto
        {
            FullName = "New Officer",
            Email = "existing@example.com",
            Password = "Password1",
            Role = "ElectionOfficer"
        };

        var result = await _sut.CreateUserAsync(request, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.DuplicateEmail, result.Error);
    }
}
