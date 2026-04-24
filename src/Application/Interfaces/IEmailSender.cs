using Donately.Application.Common.Results;

namespace Donately.Application.Interfaces;

public interface IEmailSender
{
    Task<Result> SendPasswordResetEmailAsync(SendPasswordResetEmailRequest request, CancellationToken cancellationToken = default);

    Task<Result> SendEmailVerificationEmailAsync(SendEmailVerificationEmailRequest request, CancellationToken cancellationToken = default);
}

public sealed record SendPasswordResetEmailRequest(
    string ToEmail,
    string ResetLink);

public sealed record SendEmailVerificationEmailRequest(
    string ToEmail,
    string VerificationLink);

