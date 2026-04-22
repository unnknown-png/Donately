using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Donately.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
}


