using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class UpdateBioViewModel
{
    [StringLength(1000, ErrorMessage = "Біографія не може перевищувати 1000 символів")]
    public string? Bio { get; set; }
}

