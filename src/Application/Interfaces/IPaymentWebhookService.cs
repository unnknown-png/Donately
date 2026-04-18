using System.Text.Json;
using Donately.Domain.Entities;

namespace Donately.Application.Interfaces;

public interface IPaymentWebhookService
{
    Task ConfirmDonationPaymentAsync(PaymentWebhookConfirmationRequest request, CancellationToken cancellationToken = default);
}

public record PaymentWebhookConfirmationRequest(
    Guid DonationId,
    string Provider,
    string? ProviderTransactionId,
    string TransactionStatus,
    DonationStatus DonationStatus,
    decimal Amount,
    string Currency,
    JsonDocument? Metadata,
    JsonDocument? WebhookPayload);

