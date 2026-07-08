using EVoting.Application.Common;
using EVoting.Application.DTOs.Candidates;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;

namespace EVoting.Application.Services;

public class CandidateService : ICandidateService
{
    private readonly IElectionRepository _electionRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CandidateService(
        IElectionRepository electionRepository,
        ICandidateRepository candidateRepository,
        IUnitOfWork unitOfWork)
    {
        _electionRepository = electionRepository;
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<CandidateResponseDto>>> ListCandidatesAsync(Guid electionId)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<List<CandidateResponseDto>>.Failure(AppError.NotFound, "Election not found.");
        }

        var candidates = await _candidateRepository.ListByElectionAsync(electionId);
        return Result<List<CandidateResponseDto>>.Success(candidates.Select(ToDto).ToList());
    }

    public async Task<Result<CandidateResponseDto>> CreateCandidateAsync(Guid electionId, CreateCandidateRequestDto request)
    {
        var lockCheck = await EnsureElectionIsUpcomingAsync(electionId);
        if (lockCheck is not null)
        {
            return Result<CandidateResponseDto>.Failure(lockCheck.Value.Error, lockCheck.Value.Message);
        }

        var candidate = new Candidate
        {
            ElectionId = electionId,
            Name = request.Name,
            Party = request.Party,
            PhotoUrl = request.PhotoUrl
        };

        await _candidateRepository.AddAsync(candidate);
        await _unitOfWork.SaveChangesAsync();

        return Result<CandidateResponseDto>.Success(ToDto(candidate));
    }

    public async Task<Result<CandidateResponseDto>> UpdateCandidateAsync(Guid electionId, Guid candidateId, UpdateCandidateRequestDto request)
    {
        var lockCheck = await EnsureElectionIsUpcomingAsync(electionId);
        if (lockCheck is not null)
        {
            return Result<CandidateResponseDto>.Failure(lockCheck.Value.Error, lockCheck.Value.Message);
        }

        var candidate = await _candidateRepository.GetByIdAsync(candidateId);
        if (candidate is null || candidate.ElectionId != electionId)
        {
            return Result<CandidateResponseDto>.Failure(AppError.NotFound, "Candidate not found.");
        }

        candidate.Name = request.Name;
        candidate.Party = request.Party;
        candidate.PhotoUrl = request.PhotoUrl;

        await _unitOfWork.SaveChangesAsync();

        return Result<CandidateResponseDto>.Success(ToDto(candidate));
    }

    public async Task<Result<bool>> DeleteCandidateAsync(Guid electionId, Guid candidateId)
    {
        var lockCheck = await EnsureElectionIsUpcomingAsync(electionId);
        if (lockCheck is not null)
        {
            return Result<bool>.Failure(lockCheck.Value.Error, lockCheck.Value.Message);
        }

        var candidate = await _candidateRepository.GetByIdAsync(candidateId);
        if (candidate is null || candidate.ElectionId != electionId)
        {
            return Result<bool>.Failure(AppError.NotFound, "Candidate not found.");
        }

        _candidateRepository.Remove(candidate);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private async Task<(AppError Error, string Message)?> EnsureElectionIsUpcomingAsync(Guid electionId)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return (AppError.NotFound, "Election not found.");
        }

        var status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, DateTime.UtcNow);
        if (election.Status != status)
        {
            election.Status = status;
        }

        if (status != ElectionStatus.Upcoming)
        {
            return (AppError.ElectionNotActive, "Candidates can only be managed while the election is upcoming.");
        }

        return null;
    }

    private static CandidateResponseDto ToDto(Candidate candidate) => new()
    {
        CandidateId = candidate.CandidateId,
        ElectionId = candidate.ElectionId,
        Name = candidate.Name,
        Party = candidate.Party,
        PhotoUrl = candidate.PhotoUrl
    };
}
