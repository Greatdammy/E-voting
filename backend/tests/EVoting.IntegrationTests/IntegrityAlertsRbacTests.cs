using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using EVoting.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EVoting.IntegrationTests;

public class IntegrityAlertsRbacTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IntegrityAlertsRbacTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedElectionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var admin = new User
        {
            FullName = "Seed Admin",
            Email = $"seed-admin-{Guid.NewGuid()}@example.com",
            PasswordHash = "seed",
            Role = UserRole.Administrator,
            IsVerified = true
        };
        var election = new Election
        {
            Title = "RBAC Test Election",
            Description = "Seeded for integrity-alert RBAC testing.",
            StartDate = DateTime.UtcNow.AddMinutes(-5),
            EndDate = DateTime.UtcNow.AddHours(1),
            CreatedBy = admin.UserId,
            Status = ElectionStatus.Active
        };

        context.Users.Add(admin);
        context.Elections.Add(election);
        await context.SaveChangesAsync();

        return election.ElectionId;
    }

    private string GenerateToken(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.GenerateToken(Guid.NewGuid(), role).Token;
    }

    [Fact]
    public async Task ListAlerts_ReturnsUnauthorized_WhenNoTokenProvided()
    {
        var electionId = await SeedElectionAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/admin/elections/{electionId}/integrity-alerts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListAlerts_ReturnsForbidden_ForVoterRole()
    {
        var electionId = await SeedElectionAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Voter));

        var response = await client.GetAsync($"/api/admin/elections/{electionId}/integrity-alerts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListAlerts_ReturnsOk_ForAdministratorRole()
    {
        var electionId = await SeedElectionAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Administrator));

        var response = await client.GetAsync($"/api/admin/elections/{electionId}/integrity-alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListAlerts_ReturnsOk_ForElectionOfficerRole()
    {
        var electionId = await SeedElectionAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.ElectionOfficer));

        var response = await client.GetAsync($"/api/admin/elections/{electionId}/integrity-alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReviewAlert_ReturnsBadRequest_WhenStatusIsInvalid()
    {
        var electionId = await SeedElectionAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Administrator));

        var response = await client.PostAsJsonAsync(
            $"/api/admin/elections/{electionId}/integrity-alerts/{Guid.NewGuid()}/review",
            new { status = "Open" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
