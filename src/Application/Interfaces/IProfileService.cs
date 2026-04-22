using Donately.Application.Common.Results;
using Donately.Application.ViewModels;

namespace Donately.Application.Interfaces;

public interface IProfileService
{
    Task<Result<UserProfileViewModel>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result> UpdateBioAsync(Guid userId, UpdateBioRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateDetailsAsync(Guid userId, UpdateProfileDetailsRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateLocationAsync(Guid userId, UpdateLocationRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default);
}

public sealed class UpdateBioRequest
{
    public string? Bio { get; init; }
}

public sealed class UpdateProfileDetailsRequest
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public DateTime? DateOfBirth { get; init; }
}

public sealed class UpdateLocationRequest
{
    public string? Location { get; init; }
}

public sealed class UpdateAvatarRequest
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();
}

