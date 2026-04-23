using System.ComponentModel.DataAnnotations;

namespace Donately.Application.ViewModels;

public class UpdateLocationViewModel
{
    [StringLength(200, ErrorMessage = "Локація не може перевищувати 200 символів")]
    public string? Location { get; set; }
}

