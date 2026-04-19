using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Api.Controllers;

/// <summary>
/// API endpoints for exporting data and managing exported files.
/// Requires authorization.
/// </summary>
[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ExportController(IExportService exportService, ICurrentUserService currentUserService)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>
    /// Export data to specified format and upload to MinIO.
    /// </summary>
    /// <param name="request">Export request with data and format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export file response with download URL</returns>
    /// <response code="200">File exported successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("data")]
    [ProducesResponseType(typeof(ExportFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportData(
        [FromBody] ExportDataRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest("Export request is required");

        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest("FileName is required");

        if (request.Data == null || !request.Data.Any())
            return BadRequest("Data is required and cannot be empty");

        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            var result = await _exportService.ExportDataAsync(request, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while exporting data", error = ex.Message });
        }
    }

    /// <summary>
    /// Get exported file details by ID.
    /// </summary>
    /// <param name="id">Export file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export file details</returns>
    /// <response code="200">Export file found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Export file not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExportFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExportFile(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return BadRequest("Invalid export file ID");

        try
        {
            var result = await _exportService.GetExportFileAsync(id, cancellationToken);
            if (result == null)
                return NotFound($"Export file with ID {id} not found");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving export file", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all exported files for current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of exported files</returns>
    /// <response code="200">Export files retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("my-exports")]
    [ProducesResponseType(typeof(IEnumerable<ExportFileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyExports(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            var result = await _exportService.GetUserExportFilesAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving exports", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete exported file and remove from MinIO.
    /// </summary>
    /// <param name="id">Export file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="204">Export file deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Export file not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExport(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return BadRequest("Invalid export file ID");

        try
        {
            await _exportService.DeleteExportFileAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Export file with ID {id} not found");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting export", error = ex.Message });
        }
    }
}
