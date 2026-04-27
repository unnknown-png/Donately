using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Donately.Application.Common;
using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Donately.Infrastructure.Services;

public class DonationPaymentService : IDonationPaymentService
{
    private const string ProviderName = "LiqPay";

    private static readonly HashSet<string> CompletedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "success",
        "sandbox",
        "subscribed"
    };

    private static readonly HashSet<string> RefundedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "reversed",
        "refunded"
    };

    private static readonly HashSet<string> FailedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "failure",
        "error",
        "unsubscribed"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly LiqPayOptions _liqPayOptions;
    private readonly IPaymentWebhookService _paymentWebhookService;

    public DonationPaymentService(
        ApplicationDbContext dbContext,
        IOptions<LiqPayOptions> liqPayOptions,
        IPaymentWebhookService paymentWebhookService)
    {
        _dbContext = dbContext;
        _liqPayOptions = liqPayOptions.Value;
        _paymentWebhookService = paymentWebhookService;
    }

    public async Task<Result<LiqPayCheckoutViewModel>> CreateCheckoutAsync(
        CreateDonationPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return new Error("Donation.InvalidUser", "Потрібно увійти, щоб підтримати збір.");
        }

        if (request.FundraiserId == Guid.Empty || string.IsNullOrWhiteSpace(request.FundraiserSlug))
        {
            return new Error("Donation.InvalidFundraiser", "Некоректні дані збору для оплати.");
        }

        if (request.Amount <= 0)
        {
            return new Error("Donation.InvalidAmount", "Сума донату має бути більшою за 0.");
        }

        var fundraiserSlug = request.FundraiserSlug.Trim().ToLowerInvariant();
        var fundraiser = await _dbContext.Fundraisers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.FundraiserId && x.IsActive && x.Slug == fundraiserSlug,
                cancellationToken);

        if (fundraiser is null)
        {
            return new Error("Fundraiser.NotFound", "Збір не знайдено.");
        }

        var resultUrl = BuildAbsoluteUrl($"/Fundraisers/Details?slug={Uri.EscapeDataString(fundraiserSlug)}");
        var serverUrl = BuildAbsoluteUrl("/Payment/LiqPayCallback");

        if (string.IsNullOrWhiteSpace(resultUrl) || string.IsNullOrWhiteSpace(serverUrl))
        {
            return new Error("Payment.ConfigInvalid", "Некоректно налаштовано публічну адресу застосунку для LiqPay.");
        }

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            FundraiserId = fundraiser.Id,
            DonorId = request.UserId,
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Currency = fundraiser.Currency,
            Anonymous = request.Anonymous,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = DonationStatus.Pending
        };

        var paymentTransaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Provider = ProviderName,
            Status = "created",
            Amount = donation.Amount,
            Currency = donation.Currency,
            Metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                order_id = donation.Id.ToString("N"),
                fundraiser_id = fundraiser.Id,
                fundraiser_slug = fundraiser.Slug
            }))
        };

        donation.PaymentTransactionId = paymentTransaction.Id;

        _dbContext.PaymentTransactions.Add(paymentTransaction);
        _dbContext.Donations.Add(donation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var description = BuildDescription(fundraiser.Title);
        var orderId = donation.Id.ToString("N");
        var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["public_key"] = _liqPayOptions.PublicKey,
            ["version"] = _liqPayOptions.Version,
            ["action"] = _liqPayOptions.Action,
            ["amount"] = donation.Amount.ToString("0.##", CultureInfo.InvariantCulture),
            ["currency"] = donation.Currency,
            ["description"] = description,
            ["order_id"] = orderId,
            ["result_url"] = resultUrl,
            ["server_url"] = serverUrl,
            ["sandbox"] = _liqPayOptions.Sandbox ? "1" : "0"
        });

        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeSignature(_liqPayOptions.PrivateKey, data);

        return new LiqPayCheckoutViewModel
        {
            CheckoutUrl = _liqPayOptions.CheckoutUrl,
            Data = data,
            Signature = signature,
            OrderId = orderId,
            FundraiserSlug = fundraiser.Slug
        };
    }

    public async Task<Result> HandleLiqPayCallbackAsync(LiqPayCallbackRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Data) || string.IsNullOrWhiteSpace(request.Signature))
        {
            return new Error("Payment.Callback.InvalidPayload", "Порожній callback від LiqPay.");
        }

        var expectedSignature = ComputeSignature(_liqPayOptions.PrivateKey, request.Data);
        if (!AreSignaturesEqual(expectedSignature, request.Signature))
        {
            return new Error("Payment.Callback.InvalidSignature", "Невірний підпис callback LiqPay.");
        }

        if (!TryDecodeBase64(request.Data, out var payloadJson))
        {
            return new Error("Payment.Callback.InvalidData", "Некоректний формат data у callback LiqPay.");
        }

        if (!TryParseJsonDocument(payloadJson, out var payloadDocument) || payloadDocument is null)
        {
            return new Error("Payment.Callback.InvalidJson", "Некоректний JSON у callback LiqPay.");
        }

        var payload = payloadDocument.RootElement;
        var publicKey = GetString(payload, "public_key");
        if (!string.Equals(publicKey, _liqPayOptions.PublicKey, StringComparison.Ordinal))
        {
            return new Error("Payment.Callback.PublicKeyMismatch", "Публічний ключ у callback не збігається з налаштуваннями.");
        }

        var orderId = GetString(payload, "order_id");
        if (!Guid.TryParse(orderId, out var donationId))
        {
            return new Error("Payment.Callback.InvalidOrderId", "order_id у callback не є валідним ідентифікатором донату.");
        }

        var status = GetString(payload, "status");
        if (string.IsNullOrWhiteSpace(status))
        {
            return new Error("Payment.Callback.MissingStatus", "В callback відсутній статус транзакції.");
        }

        var currency = GetString(payload, "currency").ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(currency))
        {
            return new Error("Payment.Callback.MissingCurrency", "В callback відсутня валюта транзакції.");
        }

        var amount = GetDecimal(payload, "amount");
        if (amount <= 0)
        {
            return new Error("Payment.Callback.InvalidAmount", "Некоректна сума транзакції у callback.");
        }

        var providerTransactionId = GetString(payload, "transaction_id");

        WebhookEvent? webhookEvent = null;
        if (!string.IsNullOrWhiteSpace(providerTransactionId))
        {
            webhookEvent = await _dbContext.WebhookEvents
                .SingleOrDefaultAsync(
                    x => x.Provider == ProviderName && x.ProviderEventId == providerTransactionId,
                    cancellationToken);

            if (webhookEvent?.Processed == true)
            {
                return Success.Value;
            }
        }

        webhookEvent ??= new WebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = ProviderName,
            ProviderEventId = string.IsNullOrWhiteSpace(providerTransactionId) ? null : providerTransactionId,
            ReceivedAt = DateTime.UtcNow
        };

        webhookEvent.Payload = JsonDocument.Parse(payloadJson);
        webhookEvent.Error = null;

        if (_dbContext.Entry(webhookEvent).State == EntityState.Detached)
        {
            _dbContext.WebhookEvents.Add(webhookEvent);
        }

        var confirmationResult = await _paymentWebhookService.ConfirmDonationPaymentAsync(
            new PaymentWebhookConfirmationRequest(
                DonationId: donationId,
                Provider: ProviderName,
                ProviderTransactionId: providerTransactionId,
                TransactionStatus: status,
                DonationStatus: MapDonationStatus(status),
                Amount: amount,
                Currency: currency,
                Metadata: JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    liqpay_order_id = orderId,
                    liqpay_transaction_id = providerTransactionId,
                    liqpay_status = status
                })),
                WebhookPayload: JsonDocument.Parse(payloadJson)),
            cancellationToken);

        if (confirmationResult.IsFailure)
        {
            webhookEvent.Error = confirmationResult.Error.ToString();
            webhookEvent.Processed = false;
            webhookEvent.ProcessedAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return confirmationResult.Error;
        }

        webhookEvent.Processed = true;
        webhookEvent.ProcessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success.Value;
    }

    private string BuildAbsoluteUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_liqPayOptions.PublicBaseUrl))
        {
            return string.Empty;
        }

        var baseUrl = _liqPayOptions.PublicBaseUrl.TrimEnd('/');
        if (!path.StartsWith('/'))
        {
            path = $"/{path}";
        }

        return $"{baseUrl}{path}";
    }

    private static string BuildDescription(string fundraiserTitle)
    {
        var safeTitle = string.IsNullOrWhiteSpace(fundraiserTitle) ? "збір" : fundraiserTitle.Trim();
        var description = $"Підтримка збору \"{safeTitle}\"";

        return description.Length <= 255
            ? description
            : description[..255];
    }

    private static string ComputeSignature(string privateKey, string data)
    {
        var signBytes = Encoding.UTF8.GetBytes($"{privateKey}{data}{privateKey}");
        // LiqPay signature format: base64( sha1(private_key + data + private_key) )
        var hash = SHA1.HashData(signBytes);
        return Convert.ToBase64String(hash);
    }

    private static bool AreSignaturesEqual(string expectedSignature, string actualSignature)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(actualSignature);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static DonationStatus MapDonationStatus(string providerStatus)
    {
        if (CompletedStatuses.Contains(providerStatus))
        {
            return DonationStatus.Completed;
        }

        if (RefundedStatuses.Contains(providerStatus))
        {
            return DonationStatus.Refunded;
        }

        return FailedStatuses.Contains(providerStatus)
            ? DonationStatus.Failed
            : DonationStatus.Pending;
    }

    private static string GetString(JsonElement payload, string propertyName)
    {
        if (!payload.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => property.ToString()
        };
    }

    private static decimal GetDecimal(JsonElement payload, string propertyName)
    {
        var value = GetString(payload, propertyName);

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? decimal.Round(parsed, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    private static bool TryDecodeBase64(string source, out string decoded)
    {
        decoded = string.Empty;

        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var sourceBuffer = new byte[source.Length];
        if (!Convert.TryFromBase64String(source, sourceBuffer, out var writtenBytes))
        {
            return false;
        }

        decoded = Encoding.UTF8.GetString(sourceBuffer, 0, writtenBytes);
        return true;
    }

    private static bool TryParseJsonDocument(string json, out JsonDocument? document)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var jsonReader = new Utf8JsonReader(payloadBytes, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });

        if (!JsonDocument.TryParseValue(ref jsonReader, out document))
        {
            document = null;
            return false;
        }

        if (jsonReader.BytesConsumed != payloadBytes.Length)
        {
            document.Dispose();
            document = null;
            return false;
        }

        return true;
    }
}



