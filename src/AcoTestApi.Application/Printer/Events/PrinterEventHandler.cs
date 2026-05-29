using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AcoTestApi.Application.Printer.Events;

public class PrinterEventHandler(ILogger<PrinterEventHandler> logger) 
    : INotificationHandler<PrinterConnectionChangedEvent>, 
      INotificationHandler<PrinterHardwareErrorEvent>
{
    public Task Handle(PrinterConnectionChangedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Decoupled Event Handler] Printer Connection State changed to: {State} via {Mode}", 
            notification.State, notification.Mode);
        return Task.CompletedTask;
    }

    public Task Handle(PrinterHardwareErrorEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning("[Decoupled Event Handler] Printer Hardware Error status changed to: {Error}", 
            notification.ErrorState);
        return Task.CompletedTask;
    }
}
