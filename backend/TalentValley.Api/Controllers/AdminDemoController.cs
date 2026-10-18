using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentValley.Api.Authorization;
using TalentValley.Api.DTOs;
using TalentValley.Api.Services;

namespace TalentValley.Api.Controllers;

[ApiController]
[Route("api/admin/demo")]
[Authorize(Policy = AppPolicies.RequireAdmin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminDemoController(DemoService demo) : ControllerBase
{
    // Always answers, so the admin screen knows whether to offer the demonstration tools.
    [HttpGet("estado")]
    public Task<DemoStateResponse> GetState() => demo.GetStateAsync();

    [HttpPost("preparar")]
    public Task<ActionResult<DemoChangeResponse>> Prepare() => RunAsync(demo.PrepareAsync);

    [HttpPost("restaurar")]
    public Task<ActionResult<DemoChangeResponse>> Restore() => RunAsync(demo.RestoreAsync);

    // With demonstration mode off the endpoints behave as if they did not exist.
    private async Task<ActionResult<DemoChangeResponse>> RunAsync(Func<Task<DemoChangeResponse>> action)
    {
        if (!demo.Enabled) return NotFound();
        try
        {
            return Ok(await action());
        }
        catch (DemoEmailConflictException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "O e-mail de uma conta de demonstração já pertence a uma conta real.");
        }
    }
}
