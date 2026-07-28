using EVoting.Application.DTOs.Integrity;
using EVoting.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EVoting.API.Hubs;

public class IntegrityAlertBroadcaster : IIntegrityAlertBroadcaster
{
    private readonly IHubContext<IntegrityHub> _hubContext;

    public IntegrityAlertBroadcaster(IHubContext<IntegrityHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastAlertAsync(Guid electionId, IntegrityAlertDto alert)
    {
        await _hubContext.Clients.Group(IntegrityHub.GroupName(electionId)).SendAsync("ReceiveIntegrityAlert", alert);
    }
}
