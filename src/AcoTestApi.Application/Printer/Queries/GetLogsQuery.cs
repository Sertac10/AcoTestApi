using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Printer.Queries;

public record GetLogsQuery(int? Limit = null) : IRequest<List<LogEntry>>;

public class GetLogsQueryHandler(ILoggingService loggingService) : IRequestHandler<GetLogsQuery, List<LogEntry>>
{
    public async Task<List<LogEntry>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
    {
        return await loggingService.GetLogsAsync(request.Limit);
    }
}
