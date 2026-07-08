using EVoting.Domain.Enums;

namespace EVoting.Domain.Entities;

public class Election
{
    public Guid ElectionId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ElectionStatus Status { get; set; } = ElectionStatus.Upcoming;
    public Guid CreatedBy { get; set; }

    public User? CreatedByUser { get; set; }
}
