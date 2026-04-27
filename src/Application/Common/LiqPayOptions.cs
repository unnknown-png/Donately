using System.ComponentModel.DataAnnotations;

namespace Donately.Application.Common;

public sealed class LiqPayOptions
{
    public const string SectionName = "LiqPay";

    [Required]
    [MinLength(1)]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string PrivateKey { get; set; } = string.Empty;

    [Required]
    [Url]
    public string CheckoutUrl { get; set; } = "https://www.liqpay.ua/api/3/checkout";

    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = string.Empty;

    [Range(3, 7)]
    public int Version { get; set; } = 3;

    [Required]
    [MinLength(1)]
    public string Action { get; set; } = "paydonate";

    public bool Sandbox { get; set; } = true;
}

