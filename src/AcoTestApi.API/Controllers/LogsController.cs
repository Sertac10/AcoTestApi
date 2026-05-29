using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AcoTestApi.Application.Printer.Queries;
using AcoTestApi.Application.Common.Interfaces;

namespace AcoTestApi.API.Controllers;

[ApiController]
public class LogsController(IMediator mediator, ILoggingService loggingService) : ControllerBase
{
    [HttpGet("/logs")]
    [HttpGet("/api/logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int? limit)
    {
        var logs = await mediator.Send(new GetLogsQuery(limit));
        return Ok(logs);
    }

    [HttpGet("/logs/csv")]
    [HttpGet("/api/logs/csv")]
    public async Task<IActionResult> GetLogsCsv()
    {
        var csvContent = await loggingService.ExportLogsToCsvAsync();
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        return File(bytes, "text/csv", "printer_logs.csv");
    }
}
