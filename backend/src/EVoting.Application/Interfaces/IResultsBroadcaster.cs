using EVoting.Application.DTOs.Elections;

namespace EVoting.Application.Interfaces;

public interface IResultsBroadcaster
{
    Task BroadcastResultsAsync(Guid electionId, ResultsResponseDto results);
}
