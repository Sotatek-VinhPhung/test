using System.Text.Json;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Application.Export.Messaging;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Interfaces.Export;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Messaging.Handlers;

/// <summary>
/// Handler cho topic preview-requested. Fill template → Gotenberg → PDF.
/// </summary>
public class PreviewJobHandler : IMessageHandler<ExportJobMessage>
{
    public string Topic => "preview-requested";

    private readonly IExportJobRepository _jobRepo;
    private readonly IExportService _exportService;
    private readonly IExportJobNotifier _notifier;
    private readonly IExportJobService _jobService;
    private readonly ILogger<PreviewJobHandler> _logger;

    public PreviewJobHandler(
        IExportJobRepository jobRepo,
        IExportService exportService,
        IExportJobNotifier notifier,
        IExportJobService jobService,
        ILogger<PreviewJobHandler> logger)
    {
        _jobRepo = jobRepo;
        _exportService = exportService;
        _notifier = notifier;
        _jobService = jobService;
        _logger = logger;
    }

    public async Task HandleAsync(ExportJobMessage message, CancellationToken ct)
    {
        var job = await _jobRepo.GetByIdAsync(message.JobId, ct);
        if (job is null) return;
        if (job.Status != ExportJobStatus.Pending) return;

        job.Status = ExportJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepo.UpdateAsync(job, ct);
        await _jobRepo.SaveChangesAsync(ct);

        try
        {
            var request = JsonSerializer.Deserialize<PreviewPdfRequest>(job.RequestJson)
                ?? throw new InvalidOperationException("Invalid preview request JSON");

            var result = await _exportService.GeneratePreviewPdfAsync(request, job.UserId, ct);

            job.Status = ExportJobStatus.Completed;
            job.ResultFileId = result.Id == Guid.Empty ? null : result.Id;
            job.ResultUrl = result.Url;
            job.ResultFileName = result.FileName;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepo.UpdateAsync(job, ct);
            await _jobRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Preview job {JobId} completed", job.Id);

            var status = await _jobService.GetStatusAsync(job.Id, ct);
            if (status != null)
                await _notifier.NotifyJobCompletedAsync(job.UserId, status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview job {JobId} failed", job.Id);

            job.Status = ExportJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepo.UpdateAsync(job, ct);
            await _jobRepo.SaveChangesAsync(ct);

            var status = await _jobService.GetStatusAsync(job.Id, ct);
            if (status != null)
                await _notifier.NotifyJobFailedAsync(job.UserId, status, ct);

            throw;
        }
    }
}