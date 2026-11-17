using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;

namespace PMS.API.Controllers;

/// <summary>Credentials stored against a project. Secrets are returned only by the reveal call.</summary>
[Route("api/v1/projects/{projectId:int}/credentials")]
[Authorize]
public class CredentialsController : BaseController
{
    private readonly ICredentialService _credentials;

    public CredentialsController(ICredentialService credentials)
    {
        _credentials = credentials;
    }

    [HttpGet]
    public async Task<IActionResult> List(int projectId)
        => OkResult(await _credentials.GetCredentialsAsync(projectId));

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateCredentialRequest request)
    {
        try
        {
            return CreatedResult(await _credentials.CreateCredentialAsync(projectId, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpPut("{credentialId:int}")]
    public async Task<IActionResult> Update(int projectId, int credentialId, [FromBody] UpdateCredentialRequest request)
    {
        try
        {
            var result = await _credentials.UpdateCredentialAsync(projectId, credentialId, request);
            if (result == null) return NotFoundResult("Credential not found.");
            return OkResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpDelete("{credentialId:int}")]
    public async Task<IActionResult> Delete(int projectId, int credentialId)
    {
        if (!await _credentials.DeleteCredentialAsync(projectId, credentialId))
            return NotFoundResult("Credential not found.");
        return NoContentResult();
    }

    [HttpPost("{credentialId:int}/reveal")]
    [EnableRateLimiting(AppConstants.CredentialRevealRateLimitPolicy)]
    public async Task<IActionResult> Reveal(int projectId, int credentialId)
    {
        var result = await _credentials.RevealSecretAsync(projectId, credentialId);
        if (result == null) return NotFoundResult("Credential not found.");
        Response.Headers.CacheControl = "no-store";
        return OkResult(result);
    }
}
