using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class TimeLog : AuditableEntity
{
    public string? PublicId { get; set; }
    public string? LogTitle { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? TaskId { get; set; }
    public Task? Task { get; set; }

    public int? IssueId { get; set; }
    public Issue? Issue { get; set; }

    public int? TimesheetId { get; set; }
    public Timesheet? Timesheet { get; set; }

    public DateTime Date { get; set; }
    public decimal DailyLogHours { get; set; } = 0.0m;
    public string? TimePeriod { get; set; }
    public string? Notes { get; set; }
    public string BillingType { get; set; } = "Billable";

    public int? ApprovalStatusId { get; set; }
    public MasterLookup? ApprovalStatusMaster { get; set; }

    public bool GeneralLog { get; set; } = false;
    public bool IsProcessed { get; set; } = false;

    public int? PreviousApprovalStatusId { get; set; }
    public MasterLookup? PreviousApprovalStatusMaster { get; set; }
}
