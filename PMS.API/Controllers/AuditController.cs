using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

// Read-only, matching the source (audit.py has exactly one GET route) — audit log rows
// are written by the system, never created through this API.
[Route("api/v1/audit")]
[Authorize]
public class AuditController : BaseController
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> ReadAuditLogs(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50,
        [FromQuery] string? tableName = null)
    {
        var result = await _auditService.GetAuditLogsAsync(skip, limit, tableName);
        return OkResult(result);
    }
}
