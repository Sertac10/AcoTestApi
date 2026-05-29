using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Printer.Commands;

public record PrintTextCommand(string Text, string Language = "tr", string? JobId = null) : IRequest<PrintJob>;

public class PrintTextCommandValidator : AbstractValidator<PrintTextCommand>
{
    public PrintTextCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Yazdırılacak metin boş olamaz.");
            
        RuleFor(x => x.Language)
            .Must(x => !string.IsNullOrEmpty(x) && (x.ToLower() == "tr" || x.ToLower() == "en"))
            .WithMessage("Desteklenen diller sadece 'tr' ve 'en' dilleridir.");
    }
}

public class PrintTextCommandHandler(IPrintQueueService queueService) : IRequestHandler<PrintTextCommand, PrintJob>
{
    public async Task<PrintJob> Handle(PrintTextCommand request, CancellationToken cancellationToken)
    {
        return await queueService.EnqueueJobAsync(request.Text, "text", request.Language, request.JobId);
    }
}
