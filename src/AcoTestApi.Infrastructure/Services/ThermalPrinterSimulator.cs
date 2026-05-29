using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Application.Printer.Events;
using AcoTestApi.Domain.Entities;
using AcoTestApi.Domain.Enums;
using AcoTestApi.Infrastructure.Services.Channels;
using Microsoft.Extensions.Logging;

namespace AcoTestApi.Infrastructure.Services;

public class ThermalPrinterSimulator : IThermalPrinter
{
    private readonly ILogger<ThermalPrinterSimulator> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IPredictionService _predictionService;
    private readonly PrinterChannelFactory _channelFactory;
    private readonly IMediator _mediator;

    public PrinterMode Mode { get; private set; } = PrinterMode.None;
    public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
    public PrinterErrorState ActiveError { get; private set; } = PrinterErrorState.None;

    public double RemainingPaperLengthCm { get; private set; } = 5000.0; // 50 meters
    public double TotalPaperLengthCm { get; } = 5000.0;
    public int TotalPrintedCount { get; private set; }
    public PrintJob? LastJob { get; private set; }

    private IPrinterChannel? _activeChannel;
    private CancellationTokenSource? _reconnectCts;
    private int _reconnectAttempt = 0;

    public ThermalPrinterSimulator(
        ILogger<ThermalPrinterSimulator> logger,
        ILoggingService loggingService,
        IPredictionService predictionService,
        PrinterChannelFactory channelFactory,
        IMediator mediator)
    {
        _logger = logger;
        _loggingService = loggingService;
        _predictionService = predictionService;
        _channelFactory = channelFactory;
        _mediator = mediator;
    }

    public async Task<bool> ConnectAsync(PrinterMode mode, CancellationToken cancellationToken = default)
    {
        if (mode == PrinterMode.None) return false;

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();

        Mode = mode;
        _reconnectAttempt = 0;

        _logger.LogInformation("Attempting to connect to printer via {Mode}...", mode);
        
        if (ActiveError == PrinterErrorState.COMM_ERROR)
        {
            SetState(ConnectionState.Disconnected);
            StartReconnectionLoop();
            return false;
        }

        try
        {
            // Dynamically resolve connection channel using Strategy + Factory Pattern
            _activeChannel = _channelFactory.GetChannel(mode);
            await _activeChannel.OpenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open connection channel.");
            SetState(ConnectionState.Disconnected);
            return false;
        }

        SetState(ConnectionState.Connected);
        _logger.LogInformation("Printer connected successfully on channel {Details}.", _activeChannel.ConnectionDetails);
        
        await _loggingService.LogOperationAsync(
            operation: "connect",
            connection: mode.ToString().ToLower(),
            jobId: "system",
            status: "success"
        );

        return true;
    }

    public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _reconnectCts?.Cancel();
        _reconnectAttempt = 0;
        
        var oldMode = Mode;
        Mode = PrinterMode.None;

        if (_activeChannel != null)
        {
            await _activeChannel.CloseAsync(cancellationToken);
            _activeChannel = null;
        }

        SetState(ConnectionState.Disconnected);
        _logger.LogInformation("Printer disconnected.");

        await _loggingService.LogOperationAsync(
            operation: "disconnect",
            connection: oldMode.ToString().ToLower(),
            jobId: "system",
            status: "success"
        );

        return true;
    }

    public async Task<PrintJob> PrintAsync(PrintJob job, CancellationToken cancellationToken = default)
    {
        job.Status = "Processing";

        // Check Connection
        if (ConnectionState != ConnectionState.Connected || _activeChannel == null)
        {
            job.Status = "Failed";
            job.ErrorCode = PrinterErrorState.COMM_ERROR.ToString();
            bool isEn = job.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
            job.ErrorDetail = isEn ? "Printer is not connected." : "Yazıcı bağlı değil.";
            
            await _loggingService.LogOperationAsync(
                operation: "print_" + job.ContentType,
                connection: Mode.ToString().ToLower(),
                jobId: job.JobId,
                status: "error",
                errorCode: job.ErrorCode,
                errorDetail: job.ErrorDetail
            );
            return job;
        }

        // Check Hardware Errors
        if (ActiveError != PrinterErrorState.None)
        {
            job.Status = "Failed";
            job.ErrorCode = ActiveError.ToString();
            job.ErrorDetail = GetErrorDetailMessage(ActiveError, job.Language);

            await _loggingService.LogOperationAsync(
                operation: "print_" + job.ContentType,
                connection: Mode.ToString().ToLower(),
                jobId: job.JobId,
                status: "error",
                errorCode: job.ErrorCode,
                errorDetail: job.ErrorDetail
            );
            return job;
        }

        // Calculate usage
        double jobLengthCm = _predictionService.EstimateJobLengthCm(job.Content, job.ContentType);
        double durationMs = _predictionService.EstimateJobDurationMs(jobLengthCm);

        // Check Paper Low / Paper Out
        if (RemainingPaperLengthCm < jobLengthCm)
        {
            SetSimulatedError(PrinterErrorState.PAPER_OUT);
            job.Status = "Failed";
            job.ErrorCode = PrinterErrorState.PAPER_OUT.ToString();
            job.ErrorDetail = GetErrorDetailMessage(PrinterErrorState.PAPER_OUT, job.Language);

            await _loggingService.LogOperationAsync(
                operation: "print_" + job.ContentType,
                connection: Mode.ToString().ToLower(),
                jobId: job.JobId,
                status: "error",
                errorCode: job.ErrorCode,
                errorDetail: job.ErrorDetail
            );
            return job;
        }

        // Simulate sending payload data across Strategy Connection Channel
        byte[] payload = Encoding.UTF8.GetBytes(job.Content);
        await _activeChannel.SendDataAsync(payload, cancellationToken);

        // Simulate physical printing delay
        job.EstimatedLengthCm = Math.Round(jobLengthCm, 2);
        job.ExecutionDurationMs = durationMs;
        await Task.Delay((int)durationMs, cancellationToken);

        // Generate real QR code if type is QR (cross-platform compliant)
        if (job.ContentType.Equals("qr", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(job.Content, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                string base64 = Convert.ToBase64String(qrCodeBytes);
                job.GeneratedQrCode = $"data:image/png;base64,{base64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate QR code using QRCoder.");
            }
        }

        // Reduce paper roll length
        RemainingPaperLengthCm -= jobLengthCm;
        TotalPrintedCount++;
        job.Status = "Completed";
        LastJob = job;

        await _loggingService.LogOperationAsync(
            operation: "print_" + job.ContentType,
            connection: Mode.ToString().ToLower(),
            jobId: job.JobId,
            status: "success"
        );

        return job;
    }

    public void SetSimulatedError(PrinterErrorState error)
    {
        if (ActiveError == error) return;

        ActiveError = error;
        _logger.LogWarning("Hardware error triggered: {Error}", error);
        
        Task.Run(() => _mediator.Publish(new PrinterHardwareErrorEvent(error)));

        // If communication error, disconnect immediately and start backoff retry
        if (error == PrinterErrorState.COMM_ERROR)
        {
            SetState(ConnectionState.Disconnected);
            StartReconnectionLoop();
        }
    }

    public void ResetSimulatedError()
    {
        if (ActiveError == PrinterErrorState.None) return;

        var clearedError = ActiveError;
        ActiveError = PrinterErrorState.None;
        _logger.LogInformation("Hardware error cleared: {Error}", clearedError);
        
        Task.Run(() => _mediator.Publish(new PrinterHardwareErrorEvent(PrinterErrorState.None)));

        if (clearedError == PrinterErrorState.COMM_ERROR && Mode != PrinterMode.None)
        {
            // Trigger quick reconnection
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            var token = _reconnectCts.Token;
            Task.Run(() => AttemptReconnectionAsync(token));
        }
    }

    private void SetState(ConnectionState state)
    {
        if (ConnectionState == state) return;
        ConnectionState = state;
        
        Task.Run(() => _mediator.Publish(new PrinterConnectionChangedEvent(state, Mode)));
    }

    private void StartReconnectionLoop()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        _reconnectAttempt = 0;
        
        var token = _reconnectCts.Token;
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                _reconnectAttempt++;
                
                // Exponential Backoff: 1s, 2s, 4s, 8s, 16s, max 30s
                double baseDelaySec = Math.Min(Math.Pow(2, _reconnectAttempt - 1), 30.0);
                
                // Jitter: random variation +/- 10%
                var random = new Random();
                double jitter = (random.NextDouble() * 0.2) - 0.1; // -10% to +10%
                int finalDelayMs = (int)((baseDelaySec + jitter * baseDelaySec) * 1000.0);

                _logger.LogInformation("Reconnection attempt {Attempt} in {Delay}ms...", _reconnectAttempt, finalDelayMs);
                
                try
                {
                    await Task.Delay(finalDelayMs, token);
                    var success = await AttemptReconnectionAsync(token);
                    if (success)
                    {
                        break;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }

    private async Task<bool> AttemptReconnectionAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;

        SetState(ConnectionState.Connecting);
        _logger.LogInformation("Attempting automatic reconnection...");

        if (ActiveError == PrinterErrorState.COMM_ERROR)
        {
            SetState(ConnectionState.Disconnected);
            _logger.LogWarning("Reconnection failed: COMM_ERROR active.");
            return false;
        }

        try
        {
            // Re-open concrete Strategy connection channel during automatic recovery
            if (_activeChannel == null && Mode != PrinterMode.None)
            {
                _activeChannel = _channelFactory.GetChannel(Mode);
            }

            if (_activeChannel != null)
            {
                await _activeChannel.OpenAsync(token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconnection channel open failed.");
            SetState(ConnectionState.Disconnected);
            return false;
        }

        SetState(ConnectionState.Connected);
        _reconnectAttempt = 0;
        _logger.LogInformation("Automatically reconnected to printer via {Mode}!", Mode);

        await _loggingService.LogOperationAsync(
            operation: "reconnect",
            connection: Mode.ToString().ToLower(),
            jobId: "system",
            status: "success"
        );

        return true;
    }

    private static string GetErrorDetailMessage(PrinterErrorState error, string language = "tr")
    {
        bool isEn = language.Equals("en", StringComparison.OrdinalIgnoreCase);
        return error switch
        {
            PrinterErrorState.PAPER_OUT => isEn
                ? "Paper out (PAPER_OUT). Please replace the paper roll."
                : "Kağıt bitti (PAPER_OUT). Fiş rulosunu değiştirin.",
            PrinterErrorState.PAPER_JAM => isEn
                ? "Paper jam (PAPER_JAM). Open cover and clear jam."
                : "Kağıt sıkıştı (PAPER_JAM). Yazıcı kapağını açıp temizleyin.",
            PrinterErrorState.COVER_OPEN => isEn
                ? "Printer cover open (COVER_OPEN). Please close the cover."
                : "Yazıcı kapağı açık (COVER_OPEN). Kapağı kapatın.",
            PrinterErrorState.OVERHEAT => isEn
                ? "Printer overheated (OVERHEAT). Please wait for it to cool down."
                : "Yazıcı aşırı ısındı (OVERHEAT). Soğumasını bekleyin.",
            PrinterErrorState.COMM_ERROR => isEn
                ? "Connection lost (COMM_ERROR). Please check cables."
                : "Bağlantı koptu (COMM_ERROR). Kabloları kontrol edin.",
            PrinterErrorState.UNKNOWN_COMMAND => isEn
                ? "Unknown command sent (UNKNOWN_COMMAND)."
                : "Bilinmeyen komut gönderildi (UNKNOWN_COMMAND).",
            _ => isEn
                ? "An unknown hardware error occurred."
                : "Bilinmeyen bir donanım hatası oluştu."
        };
    }
}
