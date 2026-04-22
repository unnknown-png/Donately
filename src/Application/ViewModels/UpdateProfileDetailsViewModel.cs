using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class UpdateProfileDetailsViewModel
{
    [Required(ErrorMessage = "Username обов'язковий")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username має містити від 3 до 50 символів")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обов'язковий")]
    [EmailAddress(ErrorMessage = "Некоректний email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некоректний номер телефону")]
    [StringLength(30, ErrorMessage = "Номер телефону не може перевищувати 30 символів")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
}

