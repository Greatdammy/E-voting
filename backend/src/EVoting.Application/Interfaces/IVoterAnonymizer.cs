namespace EVoting.Application.Interfaces;

public interface IVoterAnonymizer
{
    string ComputeVoterId(Guid userId);
}
