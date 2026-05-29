using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Printer.Commands;

public record PrintImageCommand(string ImageBase64, string Language = "tr", string? JobId = null) : IRequest<PrintJob>;

public class PrintImageCommandValidator : AbstractValidator<PrintImageCommand>
{
    public PrintImageCommandValidator()
    {
        RuleFor(x => x.ImageBase64)
            .NotEmpty().WithMessage("Görsel Base64 verisi boş olamaz.");

        RuleFor(x => x.Language)
            .Must(x => !string.IsNullOrEmpty(x) && (x.ToLower() == "tr" || x.ToLower() == "en"))
            .WithMessage("Desteklenen diller sadece 'tr' ve 'en' dilleridir.");
    }
}

public class PrintImageCommandHandler(IPrintQueueService queueService) : IRequestHandler<PrintImageCommand, PrintJob>
{
    public async Task<PrintJob> Handle(PrintImageCommand request, CancellationToken cancellationToken)
    {
        return await queueService.EnqueueJobAsync(request.ImageBase64, "image", request.Language, request.JobId);
    }
}
