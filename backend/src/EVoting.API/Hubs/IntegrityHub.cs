using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EVoting.API.Hubs;

[Authorize(Roles = "Administrator,ElectionOfficer")]
public class IntegrityHub : Hub
{
    public async Task JoinElection(Guid electionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(electionId));
    }

    public static string GroupName(Guid electionId) => $"integrity-{electionId}";
}
