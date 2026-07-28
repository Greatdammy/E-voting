using System.Net;
using System.Net.Http.Headers;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using EVoting.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EVoting.IntegrationTests;

public class ElectionDeletionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ElectionDeletionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid ElectionId, Guid AdminUserId)> SeedElectionAsync()
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
            Title = "Election Deletion Test Election",
            Description = "Seeded for election-deletion testing.",
            StartDate = DateTime.UtcNow.AddMinutes(-5),
            EndDate = DateTime.UtcNow.AddHours(1),
            CreatedBy = admin.UserId,
            Status = ElectionStatus.Active
        };

        context.Users.Add(admin);
        context.Elections.Add(election);
        await context.SaveChangesAsync();

        return (election.ElectionId, admin.UserId);
    }

    private async Task SeedVoteAsync(Guid electionId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var candidate = new Candidate
        {
            ElectionId = electionId,
            Name = "Candidate A",
            Party = "Independent"
        };
        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        context.Votes.Add(new Vote
        {
            ElectionId = electionId,
            CandidateId = candidate.CandidateId,
            VoterId = "seed-hashed-voter-id",
            VoteHash = "seed-vote-hash",
            VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private string GenerateToken(Guid userId, UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.GenerateToken(userId, role).Token;
    }

    [Fact]
    public async Task DeleteElection_ReturnsNoContent_ForAdministrator_WhenElectionHasNoVotes()
    {
        var (electionId, adminUserId) = await SeedElectionAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(adminUserId, UserRole.Administrator));

        var response = await client.DeleteAsync($"/api/admin/elections/{electionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await context.Elections.FindAsync(electionId));
    }

    [Fact]
    public async Task DeleteElection_ReturnsForbidden_ForElectionOfficerRole()
    {
        var (electionId, _) = await SeedElectionAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(Guid.NewGuid(), UserRole.ElectionOfficer));

        var response = await client.DeleteAsync($"/api/admin/elections/{electionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteElection_ReturnsConflict_WhenElectionHasVotes()
    {
        var (electionId, adminUserId) = await SeedElectionAsync();
        await SeedVoteAsync(electionId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(adminUserId, UserRole.Administrator));

        var response = await client.DeleteAsync($"/api/admin/elections/{electionId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(await context.Elections.FindAsync(electionId));
    }
}
