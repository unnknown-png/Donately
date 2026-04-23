using System.ComponentModel.DataAnnotations;

namespace Donately.Application.Common;

public sealed class EmailSenderOptions
{
    public const string SectionName = "SendGrid";

    [Required]
    [MinLength(1)]
    public string SendGridKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@donately.local";

    [Required]
    [MinLength(1)]
    public string FromName { get; set; } = "Donately Password Recovery";
}

