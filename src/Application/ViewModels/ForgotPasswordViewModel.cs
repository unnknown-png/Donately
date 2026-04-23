using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний email")]
    public string Email { get; set; } = string.Empty;

    public bool EmailSent { get; set; }
}

