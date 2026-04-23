using Donately.Application.Common.Results;
using Donately.Application.ViewModels;

namespace Donately.Application.Interfaces;

public interface IVerificationService
{
    Task<Result<EmailVerificationViewModel>> GetEmailVerificationAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result> SendEmailVerificationAsync(StartEmailVerificationRequest request, CancellationToken cancellationToken = default);

    Task<Result> ConfirmEmailAsync(ConfirmEmailVerificationRequest request, CancellationToken cancellationToken = default);
}

public sealed record StartEmailVerificationRequest(
    Guid UserId,
    string Email);

public sealed record ConfirmEmailVerificationRequest(
    Guid RequestId,
    string Token);

