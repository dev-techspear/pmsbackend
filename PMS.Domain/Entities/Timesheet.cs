using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Timesheet : AuditableEntity
{
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public required string UserEmail { get; set; }
    public User User { get; set; } = null!;

    public string BillingType { get; set; } = "Billable";
    public decimal TotalHours { get; set; } = 0.0m;
    public string ApprovalStatus { get; set; } = "Pending";

    public ICollection<TimeLog> Timelogs { get; set; } = new List<TimeLog>();
}
