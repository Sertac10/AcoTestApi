using System.Collections.Generic;
using System.Threading.Tasks;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Common.Interfaces;

public interface IPrintQueueService
{
    Task<PrintJob> EnqueueJobAsync(string content, string contentType, string language = "tr", string? jobId = null);
    Task<PrintJob?> GetJobStatusAsync(string jobId);
    Task<PrintJob> ReprintJobAsync(string jobId);
    List<PrintJob> GetQueueSummary();
    List<PrintJob> GetFailedJobs();
    void TriggerRecovery();
}
