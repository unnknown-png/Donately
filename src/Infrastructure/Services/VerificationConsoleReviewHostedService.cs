using Donately.Application.Interfaces;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public sealed class VerificationConsoleReviewHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<VerificationConsoleReviewHostedService> _logger;

    public VerificationConsoleReviewHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<VerificationConsoleReviewHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Console.IsInputRedirected)
        {
            _logger.LogInformation("Консольне рев'ю верифікації вимкнено: ввід з консолі недоступний.");
            return;
        }

        _logger.LogInformation("Консольне рев'ю верифікації запущено. Заявки у статусі InReview будуть запитувати рішення yes/no.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var verificationService = scope.ServiceProvider.GetRequiredService<IVerificationService>();

            var pendingRequest = await dbContext.VerificationRequests
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Status == VerificationRequestStatus.InReview)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(stoppingToken);

            if (pendingRequest is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            _logger.LogInformation(
                "Користувач {UserName} ({Email}) очікує на підтвердження верифікації. Введіть yes/no.",
                pendingRequest.User.UserName,
                pendingRequest.User.Email);

            var approved = await ReadDecisionAsync(stoppingToken);

            if (approved is null)
            {
                break;
            }

            var result = await verificationService.ReviewVerificationAsync(
                new ReviewVerificationRequest
                {
                    UserId = pendingRequest.UserId,
                    Approved = approved.Value
                },
                stoppingToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Рішення збережено: користувача {UserName} {Decision}.",
                    pendingRequest.User.UserName,
                    approved.Value ? "затверджено" : "відхилено");
                continue;
            }

            _logger.LogWarning("Не вдалося зберегти рішення: {Message}", result.Error.Message);
        }
    }

    private static async Task<bool?> ReadDecisionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("yes/no > ");
            var input = await Task.Run(Console.ReadLine, cancellationToken);

            if (input is null)
            {
                return null;
            }

            var normalized = input.Trim().ToLowerInvariant();

            if (normalized is "yes" or "y")
            {
                return true;
            }

            if (normalized is "no" or "n")
            {
                return false;
            }

            Console.WriteLine("Введи yes або no.");
        }

        return null;
    }
}


