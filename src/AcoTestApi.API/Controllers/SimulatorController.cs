using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AcoTestApi.Application.Printer.Commands;

namespace AcoTestApi.API.Controllers;

[ApiController]
public class SimulatorController(IMediator mediator) : ControllerBase
{
    [HttpPost("/api/simulator/error")]
    [HttpPost("/simulator/error")]
    public async Task<IActionResult> SimulateError([FromBody] SimulateErrorCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new { success = result, message = $"Simülatör hata durumu güncellendi: {command.ErrorCode}" });
    }
}
