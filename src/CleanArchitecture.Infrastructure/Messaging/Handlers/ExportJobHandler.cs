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
/// Handler cho topic export-requested. Xử lý Export (docx/xlsx).
/// </summary>
public class ExportJobHandler : IMessageHandler<ExportJobMessage>
{
    public string Topic => "export-requested";

    private readonly IExportJobRepository _jobRepo;
    private readonly IExportService _exportService;
    private readonly IExportJobNotifier _notifier;
    private readonly IExportJobService _jobService;
    private readonly ILogger<ExportJobHandler> _logger;

    public ExportJobHandler(
        IExportJobRepository jobRepo,
        IExportService exportService,
        IExportJobNotifier notifier,
        IExportJobService jobService,
        ILogger<ExportJobHandler> logger)
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
        if (job is null)
        {
            _logger.LogWarning("Export job {JobId} not found, skipping", message.JobId);
            return;
        }

        // Idempotency: chỉ xử lý Pending
        if (job.Status != ExportJobStatus.Pending)
        {
            _logger.LogInformation("Job {JobId} already {Status}, skipping",
                job.Id, job.Status);
            return;
        }

        // Mark processing
        job.Status = ExportJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepo.UpdateAsync(job, ct);
        await _jobRepo.SaveChangesAsync(ct);

        try
        {
            var request = JsonSerializer.Deserialize<ExportDataRequest>(job.RequestJson)
                ?? throw new InvalidOperationException("Invalid request JSON");

            var result = await _exportService.ExportDataAsync(request, job.UserId, ct);

            // Update job completed
            job.Status = ExportJobStatus.Completed;
            job.ResultFileId = result.Id;
            job.ResultUrl = result.Url;
            job.ResultFileName = result.FileName;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepo.UpdateAsync(job, ct);
            await _jobRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Export job {JobId} completed", job.Id);

            // Notify via SignalR
            var status = await _jobService.GetStatusAsync(job.Id, ct);
            if (status != null)
                await _notifier.NotifyJobCompletedAsync(job.UserId, status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job {JobId} failed", job.Id);

            job.Status = ExportJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepo.UpdateAsync(job, ct);
            await _jobRepo.SaveChangesAsync(ct);

            var status = await _jobService.GetStatusAsync(job.Id, ct);
            if (status != null)
                await _notifier.NotifyJobFailedAsync(job.UserId, status, ct);

            throw; // để KafkaConsumerService retry / DLQ
        }
    }
}