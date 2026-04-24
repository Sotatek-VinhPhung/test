using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Api.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize]
public class TemplateController : ControllerBase
{
    private readonly ITemplateManagementService _service;
    private readonly ICurrentUserService _currentUser;

    public TemplateController(ITemplateManagementService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
    [FromForm] UploadTemplateForm form,
    CancellationToken ct = default)
    {
        if (form.File == null || form.File.Length == 0)
            return BadRequest("File is required");

        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await using var stream = form.File.OpenReadStream();

        var result = await _service.UploadAsync(new UploadTemplateRequest
        {
            Name = form.Name,
            Type = form.Type,
            Description = form.Description,
            Category = form.Category,
            FileStream = stream,
            FileName = form.File.FileName
        }, userId, ct);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type = null, [FromQuery] string? category = null, CancellationToken ct = default)
        => Ok(await _service.ListAsync(type, category, ct));

    [HttpGet("{type}/{name}/versions")]
    public async Task<IActionResult> GetVersions(string type, string name, CancellationToken ct = default)
        => Ok(await _service.GetVersionsAsync(type, name, ct));

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        await _service.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
    {
        await _service.ActivateAsync(id, ct);
        return NoContent();
    }
}
// CleanArchitecture.Api/Controllers/UploadTemplateForm.cs
public class UploadTemplateForm
{
    public IFormFile File { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
}