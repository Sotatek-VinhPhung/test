using System.Text.Json;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Application.Export.Messaging;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Interfaces.Export;
using CleanArchitecture.Domain.Interfaces.Messaging;

namespace CleanArchitecture.Application.Export.Services;

public class ExportJobService : IExportJobService
{
    private readonly IExportJobRepository _jobRepo;
    private readonly IKafkaPublisher _kafka;

    // Topic constants
    public const string ExportTopic = "export-requested";
    public const string PreviewTopic = "preview-requested";

    public ExportJobService(IExportJobRepository jobRepo, IKafkaPublisher kafka)
    {
        _jobRepo = jobRepo;
        _kafka = kafka;
    }

    public async Task<Guid> EnqueueExportAsync(
        ExportDataRequest request, Guid userId, CancellationToken ct = default)
    {
        var job = new ExportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobType = ExportJobType.Export,
            Status = ExportJobStatus.Pending,
            RequestJson = JsonSerializer.Serialize(request),
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepo.AddAsync(job, ct);
        await _jobRepo.SaveChangesAsync(ct);

        await _kafka.PublishAsync(new KafkaMessage<ExportJobMessage>
        {
            Topic = ExportTopic,
            Key = job.Id.ToString(),
            Value = new ExportJobMessage { JobId = job.Id, JobType = job.JobType },
            Timestamp = DateTime.UtcNow
        }, ct);

        return job.Id;
    }

    public async Task<Guid> EnqueuePreviewAsync(
        PreviewPdfRequest request, Guid userId, CancellationToken ct = default)
    {
        var job = new ExportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobType = ExportJobType.Preview,
            Status = ExportJobStatus.Pending,
            RequestJson = JsonSerializer.Serialize(request),
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepo.AddAsync(job, ct);
        await _jobRepo.SaveChangesAsync(ct);

        await _kafka.PublishAsync(new KafkaMessage<ExportJobMessage>
        {
            Topic = PreviewTopic,
            Key = job.Id.ToString(),
            Value = new ExportJobMessage { JobId = job.Id, JobType = job.JobType },
            Timestamp = DateTime.UtcNow
        }, ct);

        return job.Id;
    }

    public async Task<ExportJobStatusResponse?> GetStatusAsync(
        Guid jobId, CancellationToken ct = default)
    {
        var job = await _jobRepo.GetByIdAsync(jobId, ct);
        if (job == null) return null;

        return new ExportJobStatusResponse
        {
            JobId = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            ResultUrl = job.ResultUrl,
            ResultFileName = job.ResultFileName,
            ResultFileId = job.ResultFileId,
            ErrorMessage = job.ErrorMessage,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }
}