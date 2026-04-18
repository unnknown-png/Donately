using System.Text.Json;

namespace Donately.Domain.Entities;

public class WebhookEvent
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderEventId { get; set; }

    public JsonDocument? Payload { get; set; }

    public DateTime ReceivedAt { get; set; }

    public bool Processed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? Error { get; set; }
}

