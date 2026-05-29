using System;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AcoTestApi.Infrastructure.Services.Channels;

public class PrinterChannelFactory(IServiceProvider serviceProvider)
{
    public virtual IPrinterChannel GetChannel(PrinterMode mode)
    {
        return mode switch
        {
            PrinterMode.Usb => serviceProvider.GetRequiredService<UsbPrinterChannel>(),
            PrinterMode.Lan => serviceProvider.GetRequiredService<LanPrinterChannel>(),
            _ => throw new ArgumentException("Geçersiz yazıcı kanalı modu.")
        };
    }
}
