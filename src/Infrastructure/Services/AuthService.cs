using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace Donately.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        LinkGenerator linkGenerator,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByNameAsync(request.UserName) is not null)
        {
            return new Error("Auth.UserNameTaken", "Користувач з таким іменем уже існує");
        }

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            return new Error("Auth.EmailTaken", "Користувач з таким email уже існує");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            NormalizedUserName = _userManager.NormalizeName(request.UserName),
            FullName = request.FullName,
            Email = request.Email,
            NormalizedEmail = _userManager.NormalizeEmail(request.Email),
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault();
            return new Error(
                $"Auth.{firstError?.Code ?? "RegistrationFailed"}",
                firstError?.Description ?? "Не вдалося зареєструвати користувача");
        }

        await _signInManager.SignInAsync(user, isPersistent: request.RememberMe);

        return Success.Value;
    }

    public async Task<Result> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return new Error("Auth.InvalidCredentials", "Невірний email або пароль");
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return new Error("Auth.InvalidCredentials", "Невірний email або пароль");
        }

        return Success.Value;
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Не розкриваємо, чи існує користувач із таким email.
        if (user is null)
        {
            return Success.Value;
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = EncodeToken(resetToken);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new Error("Auth.ResetLinkUnavailable", "Не вдалося сформувати посилання для скидання пароля");
        }

        var resetLink = _linkGenerator.GetUriByAction(
            httpContext,
            action: "ResetPassword",
            controller: "Auth",
            values: new { email = user.Email, token = encodedToken });

        if (string.IsNullOrWhiteSpace(resetLink))
        {
            return new Error("Auth.ResetLinkUnavailable", "Не вдалося сформувати посилання для скидання пароля");
        }

        var emailResult = await _emailSender.SendPasswordResetEmailAsync(
            new SendPasswordResetEmailRequest(user.Email!, resetLink),
            cancellationToken);

        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        return Success.Value;
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return new Error("Auth.InvalidResetRequest", "Невірний запит на скидання пароля");
        }

        if (!TryDecodeToken(request.Token, out var decodedToken))
        {
            return new Error("Auth.InvalidResetToken", "Недійсне посилання для скидання пароля");
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            var firstError = resetResult.Errors.FirstOrDefault();
            return new Error(
                $"Auth.{firstError?.Code ?? "PasswordResetFailed"}",
                firstError?.Description ?? "Не вдалося скинути пароль");
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return Success.Value;
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _signInManager.SignOutAsync();
        return Success.Value;
    }

    private static string EncodeToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeToken(string token, out string decodedToken)
    {
        decodedToken = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var normalized = token.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;

        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        Span<byte> buffer = new byte[normalized.Length];
        if (!Convert.TryFromBase64String(normalized, buffer, out var bytesWritten))
        {
            return false;
        }

        decodedToken = Encoding.UTF8.GetString(buffer[..bytesWritten]);
        return true;
    }
}


