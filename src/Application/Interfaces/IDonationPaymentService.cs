using Donately.Application.Common.Results;
using Donately.Application.ViewModels;

namespace Donately.Application.Interfaces;

public interface IDonationPaymentService
{
    Task<Result<LiqPayCheckoutViewModel>> CreateCheckoutAsync(CreateDonationPaymentRequest request, CancellationToken cancellationToken = default);

    Task<Result> HandleLiqPayCallbackAsync(LiqPayCallbackRequest request, CancellationToken cancellationToken = default);
}

public sealed class CreateDonationPaymentRequest
{
    public Guid UserId { get; init; }

    public Guid FundraiserId { get; init; }

    public string FundraiserSlug { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public bool Anonymous { get; init; }

    public string? Note { get; init; }
}

public sealed class LiqPayCallbackRequest
{
    public string Data { get; init; } = string.Empty;

    public string Signature { get; init; } = string.Empty;
}

