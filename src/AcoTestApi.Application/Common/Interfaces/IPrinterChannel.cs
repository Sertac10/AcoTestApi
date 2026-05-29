using System.Threading;
using System.Threading.Tasks;

namespace AcoTestApi.Application.Common.Interfaces;

public interface IPrinterChannel
{
    string ChannelType { get; } // "usb" or "lan"
    string ConnectionDetails { get; } // e.g. "VID_04B8&PID_0202" or "192.168.1.100:9100"
    Task<bool> OpenAsync(CancellationToken cancellationToken = default);
    Task<bool> CloseAsync(CancellationToken cancellationToken = default);
    Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default);
}
