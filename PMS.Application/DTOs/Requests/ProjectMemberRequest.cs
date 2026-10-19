namespace PMS.Application.DTOs.Requests;

// Matches ProjectMemberCreate in PMS-backend/app/schemas/project.py.
public record ProjectMemberRequest(
    string? UserEmail = null,
    int? UserId = null,
    string ProjectProfile = "Member",
    string PortalProfile = "User"
);
