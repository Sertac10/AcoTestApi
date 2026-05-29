using System.Threading;
using System.Threading.Tasks;
using AcoTestApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcoTestApi.Infrastructure.Services.Channels;

public class UsbPrinterChannel(ILogger<UsbPrinterChannel> logger) : IPrinterChannel
{
    public string ChannelType => "usb";
    public string ConnectionDetails => "USB001 (VID_0FE6&PID_811E - KP-300 Kiosk Printer)";

    public async Task<bool> OpenAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Opening USB Channel on {Details}...", ConnectionDetails);
        await Task.Delay(200, cancellationToken); // simulate USB handshake delay
        logger.LogInformation("USB Channel opened successfully.");
        return true;
    }

    public async Task<bool> CloseAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Closing USB Channel...");
        await Task.Delay(100, cancellationToken);
        logger.LogInformation("USB Channel closed.");
        return true;
    }

    public async Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending {Length} bytes over USB channel...", data.Length);
        await Task.Delay(50, cancellationToken); // simulate data transfer delay
        return true;
    }
}
