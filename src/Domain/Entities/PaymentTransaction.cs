using System.Text.Json;

namespace Donately.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderTransactionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "UAH";

    public JsonDocument? Metadata { get; set; }

    public JsonDocument? WebhookPayload { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
}

