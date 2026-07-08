using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EVoting.API.Hubs;

public class ResultsBroadcaster : IResultsBroadcaster
{
    private readonly IHubContext<ResultsHub> _hubContext;

    public ResultsBroadcaster(IHubContext<ResultsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastResultsAsync(Guid electionId, ResultsResponseDto results)
    {
        await _hubContext.Clients.Group(ResultsHub.GroupName(electionId)).SendAsync("ReceiveResults", results);
    }
}
