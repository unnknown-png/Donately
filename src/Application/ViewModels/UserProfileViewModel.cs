namespace Donately.Application.ViewModels;

public class UserProfileViewModel
{
    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Bio { get; init; }

    public string? Location { get; init; }

    public string? ProfileImagePath { get; init; }

    public string? PhoneNumber { get; init; }

    public DateTime? DateOfBirth { get; init; }

    public DateTime CreatedAt { get; init; }
}

