using Donately.Application.Interfaces;
using Donately.Application.Common.Results;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public class PaymentWebhookService : IPaymentWebhookService
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentWebhookService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> ConfirmDonationPaymentAsync(PaymentWebhookConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Lock donation row to avoid concurrent double-credit on repeated webhook calls.
        var donation = await _dbContext.Donations
            .FromSqlInterpolated($"SELECT * FROM \"Donations\" WHERE \"Id\" = {request.DonationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (donation is null)
        {
            return new Error("Donation.NotFound", $"Donation '{request.DonationId}' was not found.");
        }

        if (request.Amount != donation.Amount || !string.Equals(request.Currency, donation.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return new Error("Donation.AmountOrCurrencyMismatch", "Webhook amount/currency does not match donation data.");
        }

        PaymentTransaction? paymentTransaction;

        if (!string.IsNullOrWhiteSpace(request.ProviderTransactionId))
        {
            paymentTransaction = await _dbContext.PaymentTransactions
                .FirstOrDefaultAsync(
                    x => x.Provider == request.Provider &&
                         x.ProviderTransactionId == request.ProviderTransactionId,
                    cancellationToken);
        }
        else if (donation.PaymentTransactionId.HasValue)
        {
            paymentTransaction = await _dbContext.PaymentTransactions
                .FirstOrDefaultAsync(x => x.Id == donation.PaymentTransactionId.Value, cancellationToken);

            if (paymentTransaction is null)
            {
                return new Error("PaymentTransaction.NotFound", $"Payment transaction '{donation.PaymentTransactionId.Value}' was not found.");
            }
        }
        else
        {
            paymentTransaction = null;
        }

        var shouldIncreaseCurrentAmount = donation.Status != DonationStatus.Completed &&
                                          request.DonationStatus == DonationStatus.Completed;

        if (paymentTransaction is null)
        {
            paymentTransaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                Provider = request.Provider,
                ProviderTransactionId = request.ProviderTransactionId,
                Status = request.TransactionStatus,
                Amount = request.Amount,
                Currency = request.Currency,
                Metadata = request.Metadata,
                WebhookPayload = request.WebhookPayload
            };

            _dbContext.PaymentTransactions.Add(paymentTransaction);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.ProviderTransactionId))
            {
                paymentTransaction.ProviderTransactionId = request.ProviderTransactionId;
            }

            paymentTransaction.Status = request.TransactionStatus;
            paymentTransaction.Metadata = request.Metadata;
            paymentTransaction.WebhookPayload = request.WebhookPayload;
        }

        donation.Status = request.DonationStatus;
        donation.PaymentTransactionId = paymentTransaction.Id;

        if (shouldIncreaseCurrentAmount)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Fundraisers\" SET \"CurrentAmount\" = \"CurrentAmount\" + {donation.Amount} WHERE \"Id\" = {donation.FundraiserId}",
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Success.Value;
    }
}

