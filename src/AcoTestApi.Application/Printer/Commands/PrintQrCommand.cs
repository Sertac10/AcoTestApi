using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Printer.Commands;

public record PrintQrCommand(string QrData, string Language = "tr", string? JobId = null) : IRequest<PrintJob>;

public class PrintQrCommandValidator : AbstractValidator<PrintQrCommand>
{
    public PrintQrCommandValidator()
    {
        RuleFor(x => x.QrData)
            .NotEmpty().WithMessage("QR kod içeriği boş olamaz.");

        RuleFor(x => x.Language)
            .Must(x => !string.IsNullOrEmpty(x) && (x.ToLower() == "tr" || x.ToLower() == "en"))
            .WithMessage("Desteklenen diller sadece 'tr' ve 'en' dilleridir.");
    }
}

public class PrintQrCommandHandler(IPrintQueueService queueService) : IRequestHandler<PrintQrCommand, PrintJob>
{
    public async Task<PrintJob> Handle(PrintQrCommand request, CancellationToken cancellationToken)
    {
        return await queueService.EnqueueJobAsync(request.QrData, "qr", request.Language, request.JobId);
    }
}
