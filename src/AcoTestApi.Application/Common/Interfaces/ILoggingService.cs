using System.Collections.Generic;
using System.Threading.Tasks;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Common.Interfaces;

public interface ILoggingService
{
    Task LogOperationAsync(string operation, string connection, string jobId, string status, string? errorCode = null, string? errorDetail = null);
    Task<List<LogEntry>> GetLogsAsync(int? limit = null);
    Task<string> ExportLogsToCsvAsync();
}
