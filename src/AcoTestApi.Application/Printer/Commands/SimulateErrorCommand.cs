using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Enums;

namespace AcoTestApi.Application.Printer.Commands;

public record SimulateErrorCommand(string ErrorCode) : IRequest<bool>;

public class SimulateErrorCommandValidator : AbstractValidator<SimulateErrorCommand>
{
    public SimulateErrorCommandValidator()
    {
        RuleFor(x => x.ErrorCode)
            .NotEmpty().WithMessage("Hata kodu boş olamaz.")
            .Must(x => Enum.TryParse<PrinterErrorState>(x, true, out _))
            .WithMessage("Geçersiz hata kodu. Seçenekler: None, PAPER_OUT, PAPER_JAM, COVER_OPEN, OVERHEAT, COMM_ERROR, UNKNOWN_COMMAND");
    }
}

public class SimulateErrorCommandHandler(IThermalPrinter printer) : IRequestHandler<SimulateErrorCommand, bool>
{
    public Task<bool> Handle(SimulateErrorCommand request, CancellationToken cancellationToken)
    {
        if (Enum.TryParse<PrinterErrorState>(request.ErrorCode, true, out var errorState))
        {
            if (errorState == PrinterErrorState.None)
            {
                printer.ResetSimulatedError();
            }
            else
            {
                printer.SetSimulatedError(errorState);
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
