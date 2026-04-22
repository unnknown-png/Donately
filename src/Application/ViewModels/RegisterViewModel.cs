using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Вкажіть ім'я користувача")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Ім'я користувача має містити від 3 до 50 символів")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть повне ім'я")]
    [StringLength(200, ErrorMessage = "Повне ім'я не може перевищувати 200 символів")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

