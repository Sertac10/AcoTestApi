using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Printer.Queries;

public record PrinterStatusDto
{
    public string ConnectionMode { get; set; } = "none";
    public string ConnectionState { get; set; } = "Disconnected";
    public string ActiveError { get; set; } = "None";
    
    // Paper details
    public double RemainingPaperCm { get; set; }
    public double TotalPaperCm { get; set; }
    public double PaperPercentage { get; set; }
    public int EstimatedPrintsLeft { get; set; }
    
    // Statistics
    public int TotalPrintedCount { get; set; }
    public PrintJob? LastJob { get; set; }
    
    // Queue details
    public int QueueLength { get; set; }
    public double QueueTotalDurationMs { get; set; }
    public List<PrintJob> PendingJobs { get; set; } = new();
    public List<PrintJob> FailedJobs { get; set; } = new();
}

public record GetPrinterStatusQuery : IRequest<PrinterStatusDto>;

public class GetPrinterStatusQueryHandler(
    IThermalPrinter printer,
    IPrintQueueService queueService,
    IPredictionService predictionService) : IRequestHandler<GetPrinterStatusQuery, PrinterStatusDto>
{
    public Task<PrinterStatusDto> Handle(GetPrinterStatusQuery request, CancellationToken cancellationToken)
    {
        var pendingJobs = queueService.GetQueueSummary();
        var failedJobs = queueService.GetFailedJobs();
        
        var paperPct = predictionService.CalculateRemainingRollPercentage(printer.RemainingPaperLengthCm, printer.TotalPaperLengthCm);
        var printsLeft = predictionService.EstimateRemainingReceipts(printer.RemainingPaperLengthCm);
        var queueDuration = predictionService.CalculateQueueTotalDurationMs(pendingJobs);
        
        var dto = new PrinterStatusDto
        {
            ConnectionMode = printer.Mode.ToString().ToLower(),
            ConnectionState = printer.ConnectionState.ToString(),
            ActiveError = printer.ActiveError.ToString(),
            RemainingPaperCm = Math.Round(printer.RemainingPaperLengthCm, 2),
            TotalPaperCm = printer.TotalPaperLengthCm,
            PaperPercentage = Math.Round(paperPct, 2),
            EstimatedPrintsLeft = printsLeft,
            TotalPrintedCount = printer.TotalPrintedCount,
            LastJob = printer.LastJob,
            QueueLength = pendingJobs.Count,
            QueueTotalDurationMs = Math.Round(queueDuration, 2),
            PendingJobs = pendingJobs,
            FailedJobs = failedJobs
        };
        
        return Task.FromResult(dto);
    }
}
