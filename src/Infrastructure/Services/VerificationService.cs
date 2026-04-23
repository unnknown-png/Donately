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

        return new EmailVerificationViewModel
        {
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed
        };
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

        if (user.EmailConfirmed)
        {
            return new Error("Verification.EmailAlreadyConfirmed", "Пошта вже підтверджена");
        }

        var verificationRequest = await _dbContext.VerificationRequests
            .SingleOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (verificationRequest is null)
        {
            verificationRequest = new VerificationRequest
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Email = currentEmail,
                Status = VerificationRequestStatus.InReview,
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
            verificationRequest.Status = VerificationRequestStatus.InReview;
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

        user.EmailConfirmed = true;
        verificationRequest.EmailConfirmedAt = DateTime.UtcNow;
        verificationRequest.EmailConfirmationTokenHash = null;
        verificationRequest.EmailConfirmationTokenExpiresAt = null;
        verificationRequest.Status = VerificationRequestStatus.InReview;

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
}




