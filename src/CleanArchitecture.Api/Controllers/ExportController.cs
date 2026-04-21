using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Api.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
public ExportController(
    IExportService exportService,
    ICurrentUserService currentUserService)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    // ============================
    // EXPORT FILE
    // ============================
    [HttpPost("data")]
    public async Task<IActionResult> ExportData(
        [FromBody] ExportDataRequest request,
        CancellationToken cancellationToken = default)
    {
         if (request == null)
            return BadRequest("Export request is required");

        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest("FileName is required");

        //if (request.Data == null || !request.Data.Any())
        //    return BadRequest("Data is required and cannot be empty");

        if (request.Format == ExportFormat.Excel && string.IsNullOrWhiteSpace(request.TemplateName))
            return BadRequest("TemplateName is required for Excel export");

        if (request.Format == ExportFormat.Word
        && string.IsNullOrWhiteSpace(request.WordTemplateName))
            return BadRequest("WordTemplateName is required for Word export");
        try
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated");

            var result = await _exportService.ExportDataAsync(
                request,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An error occurred while exporting data",
                    error = ex.Message
                });
        }
    }

    // ============================
    // DOWNLOAD FILE (NEW)
    // ============================
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return BadRequest("Invalid export file ID");

        try
        {
            var file = await _exportService.GetExportFileAsync(id, cancellationToken);

            if (file == null)
                return NotFound($"Export file with ID {id} not found");

            // 🔥 redirect sang MinIO để tải trực tiếp
            return Redirect(file.Url);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Download failed",
                    error = ex.Message
                });
        }
    }

    // ============================
    // GET FILE DETAIL
    // ============================
    [HttpGet("{id}")]
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
                new
                {
                    message = "An error occurred while retrieving export file",
                    error = ex.Message
                });
        }
    }

    // ============================
    // GET MY EXPORTS
    // ============================
    [HttpGet("my-exports")]
    public async Task<IActionResult> GetMyExports(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated");

            var result = await _exportService.GetUserExportFilesAsync(userId, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An error occurred while retrieving exports",
                    error = ex.Message
                });
        }
    }

    // ============================
    // DELETE FILE
    // ============================
    [HttpDelete("{id}")]
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
                new
                {
                    message = "An error occurred while deleting export",
                    error = ex.Message
                });
        }
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewPdf(
    [FromBody] PreviewPdfRequest request,
    CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest("Preview request is required");

        try
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated");

            var result = await _exportService.GeneratePreviewPdfAsync(
                request, userId, cancellationToken);

            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Preview generation failed", error = ex.Message });
        }
    }
}
