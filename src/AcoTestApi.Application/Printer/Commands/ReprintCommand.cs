using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Printer.Commands;

public record ReprintCommand(string JobId) : IRequest<PrintJob>;

public class ReprintCommandHandler(IPrintQueueService queueService) : IRequestHandler<ReprintCommand, PrintJob>
{
    public async Task<PrintJob> Handle(ReprintCommand request, CancellationToken cancellationToken)
    {
        return await queueService.ReprintJobAsync(request.JobId);
    }
}
