using System.Security.Cryptography;
using System.Text;
using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public sealed class VerificationService : IVerificationService
{
    private static readonly TimeSpan VerificationLinkLifetime = TimeSpan.FromHours(24);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VerificationService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<EmailVerificationViewModel>> GetEmailVerificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Verification.UserNotFound", "Користувача не знайдено");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return new EmailVerificationViewModel
        {
            Email = user.Email ?? string.Empty,
            EmailConfirmed = verificationRequest?.EmailConfirmedAt.HasValue == true,
            VerificationStatusLabel = ResolveVerificationStatusLabel(user.VerificationStatus, verificationRequest)
        };
    }

    public async Task<Result<PhoneVerificationViewModel>> GetPhoneVerificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return new Error("Verification.UserNotFound", "Користувача не знайдено");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return new PhoneVerificationViewModel
        {
            PhoneNumber = verificationRequest?.PhoneNumber ?? user.PhoneNumber ?? string.Empty,
            PhoneConfirmed = verificationRequest?.PhoneNumberConfirmedAt.HasValue == true,
            PhoneSent = false,
            VerificationStatusLabel = ResolveVerificationStatusLabel(user.VerificationStatus, verificationRequest)
        };
    }

    public async Task<Result> SendPhoneVerificationAsync(StartPhoneVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            return new Error("Verification.UserNotFound", "Користувача не знайдено");
        }

        var normalizedPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhoneNumber))
        {
            return new Error("Verification.PhoneRequired", "Вкажіть номер телефону");
        }

        if (!IsValidE164PhoneNumber(normalizedPhoneNumber))
        {
            return new Error("Verification.PhoneInvalid", "Вкажіть номер у міжнародному форматі, наприклад +380XXXXXXXXX");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .SingleOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (verificationRequest is null)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new Error("Verification.EmailRequired", "Спочатку підтвердь email");
            }

            verificationRequest = new VerificationRequest
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Email = user.Email.Trim(),
                PhoneNumber = normalizedPhoneNumber,
                EmailConfirmedAt = DateTime.UtcNow,
                Status = VerificationRequestStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VerificationRequests.Add(verificationRequest);
        }
        else
        {
            if (!verificationRequest.EmailConfirmedAt.HasValue)
            {
                return new Error("Verification.EmailRequired", "Спочатку підтвердь email");
            }

            if (verificationRequest.Status is VerificationRequestStatus.Approved)
            {
                return new Error("Verification.AlreadyApproved", "Ваша верифікація вже підтверджена");
            }

            if (verificationRequest.Status is VerificationRequestStatus.Rejected)
            {
                return new Error("Verification.RequestRejected", "Запит на верифікацію вже відхилено");
            }

            verificationRequest.PhoneNumber = normalizedPhoneNumber;
            verificationRequest.PhoneNumberConfirmedAt = null;
            verificationRequest.Status = VerificationRequestStatus.InProgress;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Success.Value;
    }

    public async Task<Result> SendEmailVerificationAsync(StartEmailVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            return new Error("Verification.UserNotFound", "Користувача не знайдено");
        }

        var currentEmail = user.Email?.Trim() ?? string.Empty;
        var requestedEmail = request.Email.Trim();

        if (!string.Equals(currentEmail, requestedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return new Error("Verification.EmailMismatch", "Вкажіть email, який зараз прив'язаний до вашого профілю");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .SingleOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (verificationRequest?.EmailConfirmedAt.HasValue == true)
        {
            return new Error("Verification.EmailAlreadyConfirmed", "Пошта вже підтверджена");
        }

        if (verificationRequest is null)
        {
            verificationRequest = new VerificationRequest
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Email = currentEmail,
                Status = VerificationRequestStatus.NotStarted,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VerificationRequests.Add(verificationRequest);
        }
        else if (verificationRequest.Status is VerificationRequestStatus.Approved)
        {
            return new Error("Verification.AlreadyApproved", "Ваша верифікація вже підтверджена");
        }
        else if (verificationRequest.Status is VerificationRequestStatus.Rejected)
        {
            return new Error("Verification.RequestRejected", "Запит на верифікацію вже відхилено");
        }
        else
        {
            verificationRequest.Email = currentEmail;
            verificationRequest.Status = VerificationRequestStatus.NotStarted;
        }

        var token = GenerateToken();
        verificationRequest.EmailConfirmationTokenHash = HashToken(token);
        verificationRequest.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.Add(VerificationLinkLifetime);
        verificationRequest.EmailConfirmedAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new Error("Verification.LinkUnavailable", "Не вдалося сформувати посилання для підтвердження пошти");
        }

        var verificationLink = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}/Verification/ConfirmEmail?requestId={Uri.EscapeDataString(verificationRequest.Id.ToString())}&token={Uri.EscapeDataString(token)}";

        if (string.IsNullOrWhiteSpace(verificationLink))
        {
            return new Error("Verification.LinkUnavailable", "Не вдалося сформувати посилання для підтвердження пошти");
        }

        var emailResult = await _emailSender.SendEmailVerificationEmailAsync(
            new SendEmailVerificationEmailRequest(currentEmail, verificationLink),
            cancellationToken);

        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        return Success.Value;
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var verificationRequest = await _dbContext.VerificationRequests
            .SingleOrDefaultAsync(x => x.Id == request.RequestId, cancellationToken);

        if (verificationRequest is null)
        {
            return new Error("Verification.RequestNotFound", "Запит на підтвердження не знайдено");
        }

        if (verificationRequest.EmailConfirmedAt.HasValue)
        {
            return Success.Value;
        }

        if (verificationRequest.EmailConfirmationTokenHash is null ||
            verificationRequest.EmailConfirmationTokenExpiresAt is null)
        {
            return new Error("Verification.TokenMissing", "Посилання для підтвердження більше не дійсне");
        }

        if (verificationRequest.EmailConfirmationTokenExpiresAt < DateTime.UtcNow)
        {
            return new Error("Verification.TokenExpired", "Посилання для підтвердження пошти вже прострочене");
        }

        if (!string.Equals(HashToken(request.Token), verificationRequest.EmailConfirmationTokenHash, StringComparison.Ordinal))
        {
            return new Error("Verification.TokenInvalid", "Недійсне посилання для підтвердження пошти");
        }

        var user = await _userManager.FindByIdAsync(verificationRequest.UserId.ToString());

        if (user is null)
        {
            return new Error("Verification.UserNotFound", "Користувача не знайдено");
        }

        if (!string.Equals((user.Email ?? string.Empty).Trim(), verificationRequest.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return new Error("Verification.EmailChanged", "Пошта у профілі була змінена, тому цей запит більше неактуальний");
        }

        user.VerificationStatus = VerificationStatus.InProgress;
        user.IsVerified = false;
        verificationRequest.EmailConfirmedAt = DateTime.UtcNow;
        verificationRequest.EmailConfirmationTokenHash = null;
        verificationRequest.EmailConfirmationTokenExpiresAt = null;
        verificationRequest.Status = VerificationRequestStatus.InProgress;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.FirstOrDefault();
            return new Error(
                $"Verification.{error?.Code ?? "EmailConfirmationFailed"}",
                error?.Description ?? "Не вдалося підтвердити email");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Success.Value;
    }

    public async Task<Result> ConfirmPhoneAsync(ConfirmPhoneVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            return new Error("Verification.UserNotFound", "Користувача не знайдено");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .SingleOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (verificationRequest is null || !verificationRequest.EmailConfirmedAt.HasValue)
        {
            return new Error("Verification.EmailRequired", "Спочатку підтвердь email");
        }

        if (verificationRequest.PhoneNumberConfirmedAt.HasValue)
        {
            return Success.Value;
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return new Error("Verification.CodeRequired", "Вкажіть код підтвердження");
        }

        if (!string.IsNullOrWhiteSpace(verificationRequest.PhoneNumber))
        {
            user.PhoneNumber = verificationRequest.PhoneNumber.Trim();
        }

        user.VerificationStatus = VerificationStatus.InProgress;
        user.IsVerified = false;
        verificationRequest.PhoneNumberConfirmedAt = DateTime.UtcNow;
        verificationRequest.Status = VerificationRequestStatus.InProgress;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.FirstOrDefault();
            return new Error(
                $"Verification.{error?.Code ?? "PhoneConfirmationFailed"}",
                error?.Description ?? "Не вдалося підтвердити номер телефону");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Success.Value;
    }

    private static string ResolveVerificationStatusLabel(
        VerificationStatus userStatus,
        VerificationRequest? verificationRequest)
    {
        if (verificationRequest is not null)
        {
            if (verificationRequest.EmailConfirmedAt.HasValue)
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

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        return phoneNumber.Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
    }

    private static bool IsValidE164PhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.StartsWith('+'))
        {
            return false;
        }

        if (phoneNumber.Length is < 8 or > 16)
        {
            return false;
        }

        for (var i = 1; i < phoneNumber.Length; i++)
        {
            if (!char.IsDigit(phoneNumber[i]))
            {
                return false;
            }
        }

        return phoneNumber[1] is not '0';
    }
}




