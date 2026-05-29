using System;
using System.Collections.Generic;
using System.Linq;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Infrastructure.Services;

public class PredictionService : IPredictionService
{
    private const double TextLineLengthCm = 0.15;
    private const double ImageLengthCm = 2.5;
    private const double QrLengthCm = 1.5;
    private const double PrintingSpeedCmPerSec = 1.0; // 1 cm/s simulated speed

    public double EstimateJobLengthCm(string content, string contentType)
    {
        return contentType.ToLower() switch
        {
            "image" => ImageLengthCm,
            "qr" => QrLengthCm,
            "text" => CalculateTextLengthCm(content),
            _ => 1.0
        };
    }

    private static double CalculateTextLengthCm(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // Count lines or characters
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;
        return lines * TextLineLengthCm + 0.5; // Base margin of 0.5cm for receipt spacing
    }

    public double EstimateJobDurationMs(double estimatedLengthCm)
    {
        // 1 cm = 1000 ms
        var seconds = estimatedLengthCm / PrintingSpeedCmPerSec;
        return seconds * 1000.0;
    }

    public double CalculateRemainingRollPercentage(double remainingLengthCm, double totalLengthCm)
    {
        if (totalLengthCm <= 0) return 0;
        var pct = (remainingLengthCm / totalLengthCm) * 100.0;
        return Math.Max(0.0, Math.Min(100.0, pct));
    }

    public int EstimateRemainingReceipts(double remainingLengthCm, double averageReceiptLengthCm = 15.0)
    {
        if (averageReceiptLengthCm <= 0) return 0;
        return (int)Math.Floor(remainingLengthCm / averageReceiptLengthCm);
    }

    public double CalculateQueueTotalDurationMs(IEnumerable<PrintJob> pendingJobs)
    {
        return pendingJobs.Sum(j => j.EstimatedLengthCm / PrintingSpeedCmPerSec * 1000.0);
    }
}
