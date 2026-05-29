using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AcoTestApi.Application.Printer.Events;

public class PrinterRecoveryEventHandler(
    IPrintQueueService queueService,
    ILogger<PrinterRecoveryEventHandler> logger)
    : INotificationHandler<PrinterHardwareErrorEvent>,
      INotificationHandler<PrinterConnectionChangedEvent>
{
    private readonly IPrintQueueService _queueService = queueService;
    private readonly ILogger<PrinterRecoveryEventHandler> _logger = logger;

    public Task Handle(PrinterHardwareErrorEvent notification, CancellationToken cancellationToken)
    {
        if (notification.ErrorState == PrinterErrorState.None)
        {
            _logger.LogInformation("[Auto-Recovery] Hardware error cleared. Triggering queue recovery signal.");
            _queueService.TriggerRecovery();
        }
        return Task.CompletedTask;
    }

    public Task Handle(PrinterConnectionChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.State == ConnectionState.Connected)
        {
            _logger.LogInformation("[Auto-Recovery] Printer connected. Triggering queue recovery signal.");
            _queueService.TriggerRecovery();
        }
        return Task.CompletedTask;
    }
}
