using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class PhoneVerificationViewModel
{
    [Required(ErrorMessage = "Вкажіть номер телефону")]
    [RegularExpression(@"^\+[1-9]\d{7,14}$", ErrorMessage = "Вкажіть номер у міжнародному форматі, наприклад +380XXXXXXXXX")]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool PhoneSent { get; set; }

    public bool PhoneConfirmed { get; set; }

    public string VerificationStatusLabel { get; set; } = string.Empty;
}

