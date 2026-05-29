using System;
using System.Threading;
using System.Threading.Tasks;
using AcoTestApi.Domain.Entities;
using AcoTestApi.Domain.Enums;

namespace AcoTestApi.Application.Common.Interfaces;

public interface IThermalPrinter
{
    PrinterMode Mode { get; }
    ConnectionState ConnectionState { get; }
    PrinterErrorState ActiveError { get; }
    double RemainingPaperLengthCm { get; }
    double TotalPaperLengthCm { get; }
    int TotalPrintedCount { get; }
    PrintJob? LastJob { get; }

    Task<bool> ConnectAsync(PrinterMode mode, CancellationToken cancellationToken = default);
    Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);
    Task<PrintJob> PrintAsync(PrintJob job, CancellationToken cancellationToken = default);
    
    // Simulator controls
    void SetSimulatedError(PrinterErrorState error);
    void ResetSimulatedError();
}
