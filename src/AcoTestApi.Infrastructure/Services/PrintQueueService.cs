using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AcoTestApi.Infrastructure.Services;

public class PrintQueueService : BackgroundService, IPrintQueueService
{
    private readonly IThermalPrinter _printer;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<PrintQueueService> _logger;

    private readonly ConcurrentQueue<PrintJob> _queue = new();
    private readonly ConcurrentDictionary<string, PrintJob> _jobsStore = new();
    
    private readonly SemaphoreSlim _signal = new(0);
    private readonly SemaphoreSlim _recoverySignal = new(0);
    private PrintJob? _suspendedJob = null;

    public PrintQueueService(
        IThermalPrinter printer,
        IPredictionService predictionService,
        ILogger<PrintQueueService> logger)
    {
        _printer = printer;
        _predictionService = predictionService;
        _logger = logger;
    }

    public void TriggerRecovery()
    {
        try
        {
            if (_recoverySignal.CurrentCount == 0)
            {
                _recoverySignal.Release();
            }
        }
        catch (ObjectDisposedException) { }
    }

    public Task<PrintJob> EnqueueJobAsync(string content, string contentType, string language = "tr", string? jobId = null)
    {
        var finalJobId = string.IsNullOrEmpty(jobId) ? Guid.NewGuid().ToString("n")[..8] : jobId;

        // Idempotency check
        if (_jobsStore.TryGetValue(finalJobId, out var existingJob))
        {
            _logger.LogInformation("Idempotent print request received for JobId: {JobId}. Existing status: {Status}", finalJobId, existingJob.Status);
            return Task.FromResult(existingJob);
        }

        var estimatedLength = _predictionService.EstimateJobLengthCm(content, contentType);

        var job = new PrintJob
        {
            JobId = finalJobId,
            Content = content,
            ContentType = contentType,
            Language = language,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending",
            EstimatedLengthCm = Math.Round(estimatedLength, 2)
        };

        _jobsStore[finalJobId] = job;
        _queue.Enqueue(job);
        _signal.Release();

        _logger.LogInformation("Job {JobId} enqueued. Content type: {Type}", finalJobId, contentType);
        return Task.FromResult(job);
    }

    public Task<PrintJob?> GetJobStatusAsync(string jobId)
    {
        _jobsStore.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public async Task<PrintJob> ReprintJobAsync(string jobId)
    {
        if (!_jobsStore.TryGetValue(jobId, out var job))
        {
            throw new KeyNotFoundException($"Job {jobId} not found.");
        }

        _logger.LogInformation("Reprint requested for Job {JobId}", jobId);

        // Reset job status to Pending, clear errors
        job.Status = "Pending";
        job.ErrorCode = null;
        job.ErrorDetail = null;
        job.CreatedAt = DateTime.UtcNow;

        _queue.Enqueue(job);
        _signal.Release();

        return await Task.FromResult(job);
    }

    public List<PrintJob> GetQueueSummary()
    {
        var list = _queue.ToList();
        var suspended = _suspendedJob;
        if (suspended != null)
        {
            list.Insert(0, suspended);
        }
        return list;
    }

    public List<PrintJob> GetFailedJobs()
    {
        return _jobsStore.Values.Where(j => j.Status == "Failed").ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Print Queue Background Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                PrintJob? job = _suspendedJob;
                if (job == null)
                {
                    // Wait for a job to be enqueued
                    await _signal.WaitAsync(stoppingToken);

                    if (!_queue.TryDequeue(out job))
                    {
                        continue;
                    }
                }

                _logger.LogInformation("Processing print job {JobId} (Suspended: {IsSuspended})...", job.JobId, _suspendedJob != null);
                
                // Print via the simulator
                await _printer.PrintAsync(job, stoppingToken);

                _logger.LogInformation("Finished processing print job {JobId}. Final status: {Status}", job.JobId, job.Status);

                if (job.Status == "Completed")
                {
                    _suspendedJob = null;
                }
                else if (job.Status == "Failed" && IsTransientError(job.ErrorCode))
                {
                    _suspendedJob = job;
                    // Reset status so the UI shows it is Pending again/waiting
                    job.Status = "Pending";
                    job.ErrorCode = null;
                    job.ErrorDetail = null;

                    _logger.LogWarning("Job {JobId} failed with transient error. Pausing queue and waiting for recovery...", job.JobId);

                    try
                    {
                        // Wait for recovery signal or a short timeout before retrying
                        await Task.WhenAny(_recoverySignal.WaitAsync(stoppingToken), Task.Delay(3000, stoppingToken));
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                else
                {
                    // Permanent failure or other
                    _suspendedJob = null;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing print queue item.");
            }
        }

        _logger.LogInformation("Print Queue Background Processor stopped.");
    }

    private static bool IsTransientError(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode)) return false;
        return errorCode == "PAPER_OUT" ||
               errorCode == "PAPER_JAM" ||
               errorCode == "COVER_OPEN" ||
               errorCode == "OVERHEAT" ||
               errorCode == "COMM_ERROR";
    }
}
