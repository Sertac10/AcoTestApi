using System;
using System.Text.Json.Serialization;

namespace AcoTestApi.Domain.Entities;

public class LogErrorDetail
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}

public class LogEntry
{
    [JsonPropertyName("ts")]
    public string Timestamp { get; set; } = string.Empty; // ISO 8601 format

    [JsonPropertyName("op")]
    public string Operation { get; set; } = string.Empty; // e.g. "print_text", "print_image", "print_qr", "connect"

    [JsonPropertyName("conn")]
    public string Connection { get; set; } = string.Empty; // "usb", "lan", "none"

    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // "success", "error"

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LogErrorDetail? Error { get; set; }
}
