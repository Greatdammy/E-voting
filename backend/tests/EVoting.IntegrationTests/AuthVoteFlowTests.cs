using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EVoting.Application.DTOs.Auth;
using EVoting.Application.DTOs.Elections;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using EVoting.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EVoting.IntegrationTests;

public class AuthVoteFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthVoteFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid ElectionId, Guid CandidateId)> SeedElectionAsync()
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
            Title = "Integration Test Election",
            Description = "Seeded for integration testing.",
            StartDate = DateTime.UtcNow.AddMinutes(-5),
            EndDate = DateTime.UtcNow.AddHours(1),
            CreatedBy = admin.UserId,
            Status = ElectionStatus.Active
        };
        var candidate = new Candidate
        {
            ElectionId = election.ElectionId,
            Name = "Candidate A",
            Party = "Independent"
        };

        context.Users.Add(admin);
        context.Elections.Add(election);
        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        return (election.ElectionId, candidate.CandidateId);
    }

    [Fact]
    public async Task RegisterLoginVoteResults_FullChain_Succeeds()
    {
        var (electionId, candidateId) = await SeedElectionAsync();
        var client = _factory.CreateClient();
        var email = $"voter-{Guid.NewGuid()}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Integration Voter",
            email,
            password = "Password1"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

        // Voting is gated on an OTP now: submitting without one must fail...
        var voteWithoutOtpResponse = await client.PostAsJsonAsync($"/api/elections/{electionId}/vote", new { candidateId, otpCode = "" });
        Assert.Equal(HttpStatusCode.BadRequest, voteWithoutOtpResponse.StatusCode);

        // ...requesting one emails a code (captured by the TestEmailService double)...
        var otpRequestResponse = await client.PostAsync($"/api/elections/{electionId}/otp/request", null);
        Assert.Equal(HttpStatusCode.OK, otpRequestResponse.StatusCode);
        var otpRequest = await otpRequestResponse.Content.ReadFromJsonAsync<RequestOtpResponseDto>();
        Assert.NotNull(otpRequest);
        Assert.Contains("***", otpRequest!.MaskedEmail);

        var code = _factory.EmailService.GetLastCode(email);

        // ...and a wrong code is rejected without spending the real one.
        var wrongCodeResponse = await client.PostAsJsonAsync($"/api/elections/{electionId}/vote", new { candidateId, otpCode = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, wrongCodeResponse.StatusCode);

        var voteResponse = await client.PostAsJsonAsync($"/api/elections/{electionId}/vote", new { candidateId, otpCode = code });
        Assert.Equal(HttpStatusCode.OK, voteResponse.StatusCode);
        var vote = await voteResponse.Content.ReadFromJsonAsync<CastVoteResponseDto>();
        Assert.NotNull(vote);
        Assert.NotEqual(Guid.Empty, vote!.VoteId);
        Assert.False(string.IsNullOrWhiteSpace(vote.ConfirmationHash));

        var resultsClient = _factory.CreateClient();
        var resultsResponse = await resultsClient.GetAsync($"/api/elections/{electionId}/results");
        Assert.Equal(HttpStatusCode.OK, resultsResponse.StatusCode);
        var results = await resultsResponse.Content.ReadFromJsonAsync<ResultsResponseDto>();
        Assert.NotNull(results);
        Assert.Equal(1, results!.TotalVotes);
        Assert.Contains(results.Tally, t => t.CandidateId == candidateId && t.VoteCount == 1);

        // The spent code can't be replayed even against the (now-failing) second vote attempt.
        var secondVoteResponse = await client.PostAsJsonAsync($"/api/elections/{electionId}/vote", new { candidateId, otpCode = code });
        Assert.Equal(HttpStatusCode.Conflict, secondVoteResponse.StatusCode);
    }
}
