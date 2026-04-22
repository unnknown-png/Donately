using Donately.Application.Common.Results;

namespace Donately.Application.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}

public sealed record RegisterUserRequest(
    string UserName,
    string FullName,
    string Email,
    string Password,
    bool RememberMe);

