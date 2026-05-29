using System.Threading;
using System.Threading.Tasks;
using AcoTestApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcoTestApi.Infrastructure.Services.Channels;

public class LanPrinterChannel(ILogger<LanPrinterChannel> logger) : IPrinterChannel
{
    public string ChannelType => "lan";
    public string ConnectionDetails => "192.168.1.100:9100 (KP-301H/302 Kiosk Network Socket)";

    public async Task<bool> OpenAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Opening TCP socket connection on {Details}...", ConnectionDetails);
        await Task.Delay(400, cancellationToken); // simulate network socket handshake delay (slightly longer than USB)
        logger.LogInformation("TCP Socket connected successfully.");
        return true;
    }

    public async Task<bool> CloseAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Closing TCP Socket...");
        await Task.Delay(100, cancellationToken);
        logger.LogInformation("TCP Socket closed.");
        return true;
    }

    public async Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending {Length} bytes over TCP socket channel...", data.Length);
        await Task.Delay(80, cancellationToken); // simulate network latency
        return true;
    }
}
