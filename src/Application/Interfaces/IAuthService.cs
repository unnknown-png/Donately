using Donately.Application.Common.Results;

namespace Donately.Application.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    Task<Result> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default);

    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(CancellationToken cancellationToken = default);
}

public sealed record RegisterUserRequest(
    string UserName,
    string FullName,
    string Email,
    string Password,
    bool RememberMe);

public sealed record LoginUserRequest(
    string Email,
    string Password,
    bool RememberMe);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

