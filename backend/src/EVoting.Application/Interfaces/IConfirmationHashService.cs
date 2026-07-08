namespace EVoting.Application.Interfaces;

public interface IConfirmationHashService
{
    string Compute(Guid voteId, Guid electionId);
}
