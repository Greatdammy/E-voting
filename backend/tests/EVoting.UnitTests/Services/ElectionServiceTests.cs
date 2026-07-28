using EVoting.Application.Common;
using EVoting.Application.Interfaces;
using EVoting.Application.Services;
using EVoting.Domain.Entities;
using Moq;
using Xunit;

namespace EVoting.UnitTests.Services;

public class ElectionServiceTests
{
    private readonly Mock<IElectionRepository> _electionRepository = new();
    private readonly Mock<ICandidateRepository> _candidateRepository = new();
    private readonly Mock<IVoterElectionStatusRepository> _voterElectionStatusRepository = new();
    private readonly Mock<IVoteRepository> _voteRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ElectionService _sut;

    public ElectionServiceTests()
    {
        _sut = new ElectionService(
            _electionRepository.Object,
            _candidateRepository.Object,
            _voterElectionStatusRepository.Object,
            _voteRepository.Object,
            _auditLogService.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task DeleteElectionAsync_ReturnsNotFound_WhenElectionDoesNotExist()
    {
        var electionId = Guid.NewGuid();
        _electionRepository.Setup(r => r.GetByIdAsync(electionId)).ReturnsAsync((Election?)null);

        var result = await _sut.DeleteElectionAsync(electionId, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.NotFound, result.Error);
        _electionRepository.Verify(r => r.Remove(It.IsAny<Election>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteElectionAsync_ReturnsElectionHasVotes_WhenVotesExist()
    {
        var election = new Election();
        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voteRepository.Setup(r => r.HasVotesAsync(election.ElectionId)).ReturnsAsync(true);

        var result = await _sut.DeleteElectionAsync(election.ElectionId, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.ElectionHasVotes, result.Error);
        _electionRepository.Verify(r => r.Remove(It.IsAny<Election>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteElectionAsync_Succeeds_WhenNoVotesExist()
    {
        var election = new Election();
        var deletedBy = Guid.NewGuid();
        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voteRepository.Setup(r => r.HasVotesAsync(election.ElectionId)).ReturnsAsync(false);

        var result = await _sut.DeleteElectionAsync(election.ElectionId, deletedBy);

        Assert.True(result.Succeeded);
        _electionRepository.Verify(r => r.Remove(election), Times.Once);
        _auditLogService.Verify(a => a.LogAsync(deletedBy, It.IsAny<string>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
