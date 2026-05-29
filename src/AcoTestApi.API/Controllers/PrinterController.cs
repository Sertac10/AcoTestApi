using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AcoTestApi.Application.Printer.Commands;
using AcoTestApi.Application.Printer.Queries;

namespace AcoTestApi.API.Controllers;

[ApiController]
public class PrinterController(IMediator mediator) : ControllerBase
{
    [HttpPost("/connect")]
    [HttpPost("/api/printer/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new { success = result, message = "Yazıcı bağlantısı başarıyla kuruldu." });
    }

    [HttpPost("/print/text")]
    [HttpPost("/api/printer/print/text")]
    public async Task<IActionResult> PrintText([FromBody] PrintTextCommand command)
    {
        var job = await mediator.Send(command);
        return Ok(job);
    }

    [HttpPost("/print/image")]
    [HttpPost("/api/printer/print/image")]
    public async Task<IActionResult> PrintImage([FromBody] PrintImageCommand command)
    {
        var job = await mediator.Send(command);
        return Ok(job);
    }

    [HttpPost("/print/qr")]
    [HttpPost("/api/printer/print/qr")]
    public async Task<IActionResult> PrintQr([FromBody] PrintQrCommand command)
    {
        var job = await mediator.Send(command);
        return Ok(job);
    }

    [HttpGet("/status")]
    [HttpGet("/api/printer/status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await mediator.Send(new GetPrinterStatusQuery());
        return Ok(status);
    }

    [HttpPost("/reprint")]
    [HttpPost("/api/printer/reprint")]
    public async Task<IActionResult> Reprint([FromBody] ReprintCommand command)
    {
        var job = await mediator.Send(command);
        return Ok(job);
    }
}
