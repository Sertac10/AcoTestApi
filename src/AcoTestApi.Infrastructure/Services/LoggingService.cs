using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Infrastructure.Services;

public class LoggingService : ILoggingService
{
    private readonly string _logFilePath;
    private static readonly object LockObj = new();

    public LoggingService()
    {
        // Place logs.json in the application root directory
        _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.json");
    }

    public async Task LogOperationAsync(string operation, string connection, string jobId, string status, string? errorCode = null, string? errorDetail = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Operation = operation,
            Connection = connection,
            JobId = jobId,
            Status = status,
            Error = string.IsNullOrEmpty(errorCode) ? null : new LogErrorDetail
            {
                Code = errorCode,
                Detail = errorDetail ?? string.Empty
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var jsonLine = JsonSerializer.Serialize(entry, options);

        await Task.Run(() =>
        {
            lock (LockObj)
            {
                File.AppendAllText(_logFilePath, jsonLine + Environment.NewLine);
            }
        });
    }

    public async Task<List<LogEntry>> GetLogsAsync(int? limit = null)
    {
        if (!File.Exists(_logFilePath))
        {
            return new List<LogEntry>();
        }

        var logs = new List<LogEntry>();

        await Task.Run(() =>
        {
            lock (LockObj)
            {
                try
                {
                    using var fileStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var entry = JsonSerializer.Deserialize<LogEntry>(line);
                            if (entry != null)
                            {
                                logs.Add(entry);
                            }
                        }
                        catch
                        {
                            // Skip corrupt lines silently
                        }
                    }
                }
                catch
                {
                    // Handle file reading failures gracefully
                }
            }
        });

        // Return reverse chronological order (newest first)
        logs.Reverse();
        
        if (limit.HasValue)
        {
            return logs.Take(limit.Value).ToList();
        }

        return logs;
    }

    public async Task<string> ExportLogsToCsvAsync()
    {
        var logs = await GetLogsAsync();
        
        var csv = new StringBuilder();
        // Header
        csv.AppendLine("Timestamp,Operation,Connection,JobId,Status,ErrorCode,ErrorDetail");

        foreach (var log in logs)
        {
            var code = log.Error?.Code ?? "";
            var detail = log.Error?.Detail ?? "";
            
            // Escape values containing commas or quotes
            var timestamp = EscapeCsv(log.Timestamp);
            var operation = EscapeCsv(log.Operation);
            var connection = EscapeCsv(log.Connection);
            var jobId = EscapeCsv(log.JobId);
            var status = EscapeCsv(log.Status);
            code = EscapeCsv(code);
            detail = EscapeCsv(detail);

            csv.AppendLine($"{timestamp},{operation},{connection},{jobId},{status},{code},{detail}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
        {
            return "\"" + val.Replace("\"", "\"\"") + "\"";
        }
        return val;
    }
}
