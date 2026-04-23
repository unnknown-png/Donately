using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class CompletePhoneVerificationViewModel
{
    [Required]
    public string Code { get; set; } = string.Empty;
}

