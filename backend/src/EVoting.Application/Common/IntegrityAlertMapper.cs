using EVoting.Application.DTOs.Integrity;
using EVoting.Domain.Entities;

namespace EVoting.Application.Common;

public static class IntegrityAlertMapper
{
    public static IntegrityAlertDto ToDto(IntegrityAlert alert) => new()
    {
        AlertId = alert.AlertId,
        ElectionId = alert.ElectionId,
        AlertType = alert.AlertType.ToString(),
        Severity = alert.Severity.ToString(),
        DetectedAt = alert.DetectedAt,
        WindowStart = alert.WindowStart,
        WindowEnd = alert.WindowEnd,
        ObservedValue = alert.ObservedValue,
        BaselineValue = alert.BaselineValue,
        Status = alert.Status.ToString(),
        ReviewedBy = alert.ReviewedBy,
        ReviewedAt = alert.ReviewedAt,
        ReviewNote = alert.ReviewNote
    };
}
