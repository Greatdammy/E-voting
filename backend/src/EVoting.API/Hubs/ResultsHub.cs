using Microsoft.AspNetCore.SignalR;

namespace EVoting.API.Hubs;

public class ResultsHub : Hub
{
    public async Task JoinElection(Guid electionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(electionId));
    }

    public static string GroupName(Guid electionId) => $"election-{electionId}";
}
