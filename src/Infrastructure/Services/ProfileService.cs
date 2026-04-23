using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;

    private static readonly HashSet<string> AllowedAvatarExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    private static readonly HashSet<string> AllowedAvatarContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const int MaxAvatarSizeInBytes = 2 * 1024 * 1024;

    public ProfileService(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<Result<UserProfileViewModel>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Profile.NotFound", "Користувача не знайдено");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var emailConfirmed = verificationRequest?.EmailConfirmedAt.HasValue == true;
        var phoneConfirmed = verificationRequest?.PhoneNumberConfirmedAt.HasValue == true;
        var verificationStatusLabel = ResolveVerificationStatusLabel(user.VerificationStatus, verificationRequest, emailConfirmed);

        return new UserProfileViewModel
        {
            UserName = user.UserName ?? "—",
            FullName = string.IsNullOrWhiteSpace(user.FullName) ? "—" : user.FullName,
            Email = user.Email ?? "—",
            Bio = user.Bio,
            Location = user.Location,
            ProfileImagePath = user.ProfileImagePath,
            PhoneNumber = user.PhoneNumber,
            PhoneConfirmed = phoneConfirmed,
            DateOfBirth = user.DateOfBirth,
            EmailConfirmed = emailConfirmed,
            VerificationStatusLabel = verificationStatusLabel,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<Result> UpdateBioAsync(Guid userId, UpdateBioRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Profile.NotFound", "Користувача не знайдено");
        }

        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.FirstOrDefault();
            return new Error(
                $"Profile.{error?.Code ?? "UpdateFailed"}",
                error?.Description ?? "Не вдалося оновити біографію");
        }

        return Success.Value;
    }

    public async Task<Result> UpdateDetailsAsync(Guid userId, UpdateProfileDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Profile.NotFound", "Користувача не знайдено");
        }

        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserName) || string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return new Error("Profile.Validation", "Username та Email обов'язкові");
        }

        var existingUserByName = await _userManager.FindByNameAsync(normalizedUserName);
        if (existingUserByName is not null && existingUserByName.Id != userId)
        {
            return new Error("Profile.UserNameTaken", "Користувач з таким username вже існує");
        }

        var existingUserByEmail = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUserByEmail is not null && existingUserByEmail.Id != userId)
        {
            return new Error("Profile.EmailTaken", "Користувач з таким email вже існує");
        }

        var emailChanged = !string.Equals(user.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var normalizedPhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var phoneChanged = !string.Equals(user.PhoneNumber?.Trim(), normalizedPhoneNumber, StringComparison.OrdinalIgnoreCase);

        var verificationRequest = await _dbContext.VerificationRequests
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (verificationRequest is not null && emailChanged)
        {
            verificationRequest.Email = normalizedEmail;
            verificationRequest.EmailConfirmationTokenHash = null;
            verificationRequest.EmailConfirmationTokenExpiresAt = null;
            verificationRequest.EmailConfirmedAt = null;
            verificationRequest.Status = VerificationRequestStatus.NotStarted;

            user.IsVerified = false;
            user.VerificationStatus = VerificationStatus.NotStarted;
        }

        if (verificationRequest is not null && phoneChanged)
        {
            verificationRequest.PhoneNumber = normalizedPhoneNumber;
            verificationRequest.PhoneNumberConfirmedAt = null;
        }

        if (verificationRequest is null && (emailChanged || phoneChanged))
        {
            verificationRequest = new VerificationRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = normalizedEmail,
                PhoneNumber = normalizedPhoneNumber,
                Status = VerificationRequestStatus.NotStarted,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VerificationRequests.Add(verificationRequest);

            user.IsVerified = false;
            user.VerificationStatus = VerificationStatus.NotStarted;
        }

        user.UserName = normalizedUserName;
        user.NormalizedUserName = _userManager.NormalizeName(normalizedUserName);
        user.Email = normalizedEmail;
        user.NormalizedEmail = _userManager.NormalizeEmail(normalizedEmail);
        user.PhoneNumber = normalizedPhoneNumber;
        user.DateOfBirth = request.DateOfBirth?.Date;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.FirstOrDefault();
            return new Error(
                $"Profile.{error?.Code ?? "UpdateFailed"}",
                error?.Description ?? "Не вдалося оновити профіль");
        }

        if (verificationRequest is not null)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Success.Value;
    }

    public async Task<Result> UpdateLocationAsync(Guid userId, UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Profile.NotFound", "Користувача не знайдено");
        }

        user.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.FirstOrDefault();
            return new Error(
                $"Profile.{error?.Code ?? "UpdateFailed"}",
                error?.Description ?? "Не вдалося оновити локацію");
        }

        return Success.Value;
    }

    public async Task<Result> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Profile.NotFound", "Користувача не знайдено");
        }

        if (request.Content.Length == 0)
        {
            return new Error("Profile.AvatarEmpty", "Файл аватара порожній");
        }

        if (request.Content.Length > MaxAvatarSizeInBytes)
        {
            return new Error("Profile.AvatarTooLarge", "Аватар має бути менше 2 МБ");
        }

        var fileExtension = Path.GetExtension(request.FileName).ToLowerInvariant();

        if (!AllowedAvatarExtensions.Contains(fileExtension) ||
            !AllowedAvatarContentTypes.Contains(request.ContentType))
        {
            return new Error("Profile.AvatarInvalidType", "Дозволені формати: jpg, jpeg, png, webp");
        }

        var webRootPath = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            return new Error("Profile.StorageUnavailable", "Сховище файлів недоступне");
        }

        var avatarsFolder = Path.Combine(webRootPath, "uploads", "avatars");
        Directory.CreateDirectory(avatarsFolder);

        var newFileName = $"{userId:N}_{Guid.NewGuid():N}{fileExtension}";
        var newFilePath = Path.Combine(avatarsFolder, newFileName);

        await File.WriteAllBytesAsync(newFilePath, request.Content, cancellationToken);

        var previousRelativePath = user.ProfileImagePath;
        user.ProfileImagePath = $"/uploads/avatars/{newFileName}";

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.FirstOrDefault();
            return new Error(
                $"Profile.{error?.Code ?? "UpdateFailed"}",
                error?.Description ?? "Не вдалося оновити аватар");
        }

        DeletePreviousAvatarIfNeeded(webRootPath, previousRelativePath, user.ProfileImagePath);

        return Success.Value;
    }

    private static void DeletePreviousAvatarIfNeeded(string webRootPath, string? previousRelativePath, string currentRelativePath)
    {
        if (string.IsNullOrWhiteSpace(previousRelativePath) ||
            string.Equals(previousRelativePath, currentRelativePath, StringComparison.OrdinalIgnoreCase) ||
            !previousRelativePath.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var normalized = previousRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var previousAbsolutePath = Path.Combine(webRootPath, normalized);

        if (File.Exists(previousAbsolutePath))
        {
            File.Delete(previousAbsolutePath);
        }
    }

    private static string ResolveVerificationStatusLabel(
        VerificationStatus userStatus,
        VerificationRequest? verificationRequest,
        bool emailConfirmed)
    {
        if (verificationRequest is not null)
        {
            if (emailConfirmed)
            {
                return verificationRequest.Status switch
                {
                    VerificationRequestStatus.Approved => "Підтверджено",
                    VerificationRequestStatus.Rejected => "Відхилено",
                    VerificationRequestStatus.NeedsRevision => "Потребує виправлень",
                    _ => "В процесі"
                };
            }

            return verificationRequest.Status switch
            {
                VerificationRequestStatus.NotStarted => "Не розпочато",
                VerificationRequestStatus.InProgress => "В процесі",
                VerificationRequestStatus.InReview => "На перевірці",
                VerificationRequestStatus.Approved => "Підтверджено",
                VerificationRequestStatus.Rejected => "Відхилено",
                VerificationRequestStatus.NeedsRevision => "Потребує виправлень",
                _ => "Не розпочато"
            };
        }

        return userStatus switch
        {
            VerificationStatus.NotStarted => "Не розпочато",
            VerificationStatus.InProgress => "В процесі",
            VerificationStatus.InReview => "На перевірці",
            VerificationStatus.Approved => "Підтверджено",
            VerificationStatus.Rejected => "Відхилено",
            VerificationStatus.NeedsRevision => "Потребує виправлень",
            _ => "Не розпочато"
        };
    }
}

