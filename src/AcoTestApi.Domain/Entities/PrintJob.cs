using System;

namespace AcoTestApi.Domain.Entities;

public class PrintJob
{
    public string JobId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // "text", "image", "qr"
    public string Language { get; set; } = "tr";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // "Pending", "Processing", "Completed", "Failed"
    public string? ErrorCode { get; set; }
    public string? ErrorDetail { get; set; }
    public double EstimatedLengthCm { get; set; }
    public double ExecutionDurationMs { get; set; }
    public string? GeneratedQrCode { get; set; }
}
