using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class EmailVerificationViewModel
{
    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний email")]
    public string Email { get; set; } = string.Empty;

    public bool EmailSent { get; set; }

    public bool EmailConfirmed { get; set; }
}

