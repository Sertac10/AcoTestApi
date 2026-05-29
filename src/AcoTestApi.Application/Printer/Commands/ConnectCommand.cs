using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Application.Common.Exceptions;
using AcoTestApi.Domain.Enums;

namespace AcoTestApi.Application.Printer.Commands;

public record ConnectCommand(string Mode) : IRequest<bool>;

public class ConnectCommandValidator : AbstractValidator<ConnectCommand>
{
    public ConnectCommandValidator()
    {
        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("Bağlantı modu boş olamaz.")
            .Must(x => x.ToLower() == "usb" || x.ToLower() == "lan" || x.ToLower() == "none")
            .WithMessage("Bağlantı modu sadece 'usb', 'lan' veya 'none' olabilir.");
    }
}

public class ConnectCommandHandler(IThermalPrinter printer) : IRequestHandler<ConnectCommand, bool>
{
    public async Task<bool> Handle(ConnectCommand request, CancellationToken cancellationToken)
    {
        var reqMode = request.Mode.ToLower();
        
        if (reqMode == "none")
        {
            return await printer.DisconnectAsync(cancellationToken);
        }

        var mode = reqMode == "lan" ? PrinterMode.Lan : PrinterMode.Usb;
        var result = await printer.ConnectAsync(mode, cancellationToken);
        
        // If the printer connection fails immediately (e.g. simulated error), we throw AppException
        // but backoff will work in background!
        if (!result)
        {
            throw new AppException(HttpStatusCode.ServiceUnavailable, "Yazıcı bağlantısı kurulamadı. Otomatik yeniden bağlanma süreci (backoff) başlatıldı.");
        }
        return result;
    }
}
