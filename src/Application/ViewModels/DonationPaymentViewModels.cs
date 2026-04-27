using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public sealed class StartDonationPaymentViewModel
{
    [Required]
    public Guid FundraiserId { get; set; }

    [Required]
    [MinLength(1)]
    public string FundraiserSlug { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "999999999", ErrorMessage = "Сума донату має бути більшою за 0")]
    public decimal Amount { get; set; }

    public bool Anonymous { get; set; }

    [StringLength(500, ErrorMessage = "Коментар до донату має містити до 500 символів")]
    public string? Note { get; set; }
}

public sealed class LiqPayCheckoutViewModel
{
    public string CheckoutUrl { get; init; } = "https://www.liqpay.ua/api/3/checkout";

    public string Data { get; init; } = string.Empty;

    public string Signature { get; init; } = string.Empty;

    public string OrderId { get; init; } = string.Empty;

    public string FundraiserSlug { get; init; } = string.Empty;
}

