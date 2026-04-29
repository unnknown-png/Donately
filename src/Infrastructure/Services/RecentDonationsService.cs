using System.Globalization;
using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public sealed class RecentDonationsService : IRecentDonationsService
{
    private const int DefaultLimit = 6;
    private const int MaxLimit = 12;

    private readonly ApplicationDbContext _dbContext;

    public RecentDonationsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<RecentDonationsFeedViewModel>> GetLatestAsync(int limit = DefaultLimit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, MaxLimit);
        var utcNow = DateTime.UtcNow;

        var donations = await _dbContext.Donations
            .AsNoTracking()
            .Where(x => x.Status == DonationStatus.Completed)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Amount,
                x.Currency,
                x.Anonymous,
                x.CreatedAt,
                DonorUserName = x.Anonymous || x.Donor == null
                    ? null
                    : x.Donor.FullName,
                DonorNickName = x.Anonymous || x.Donor == null
                    ? null
                    : x.Donor.UserName,
                DonorProfileImagePath = x.Anonymous || x.Donor == null
                    ? null
                    : x.Donor.ProfileImagePath,
                FundraiserTitle = x.Fundraiser.Title,
                FundraiserSlug = x.Fundraiser.Slug
            })
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = donations.Select(donation =>
        {
            var donorName = ResolveDonorName(donation.Anonymous, donation.DonorUserName, donation.DonorNickName);
            var donorInitial = BuildInitial(donorName, donation.Anonymous);

            return new RecentDonationCardViewModel
            {
                Id = donation.Id,
                DonorName = donorName,
                DonorInitial = donorInitial,
                DonorProfileImagePath = donation.DonorProfileImagePath,
                FundraiserTitle = donation.FundraiserTitle,
                FundraiserSlug = donation.FundraiserSlug,
                AmountLabel = FormatAmount(donation.Amount, donation.Currency),
                CreatedAt = donation.CreatedAt,
                TimeLabel = BuildRelativeTimeLabel(donation.CreatedAt, utcNow)
            };
        }).ToList();

        return new RecentDonationsFeedViewModel
        {
            Items = items,
            RefreshedAt = DateTimeOffset.UtcNow
        };
    }

    private static string ResolveDonorName(bool anonymous, string? fullName, string? userName)
    {
        if (anonymous)
        {
            return "Анонімний донат";
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        return "Користувач";
    }

    private static string BuildInitial(string donorName, bool anonymous)
    {
        if (anonymous)
        {
            return "А";
        }

        var firstLetter = donorName.Trim().FirstOrDefault(char.IsLetterOrDigit);
        return firstLetter == default ? "U" : char.ToUpperInvariant(firstLetter).ToString();
    }

    private static string FormatAmount(decimal amount, string currency)
    {
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "UAH" : currency.Trim().ToUpperInvariant();
        return $"{amount.ToString("0.##", culture)} {normalizedCurrency}";
    }

    private static string BuildRelativeTimeLabel(DateTime createdAtUtc, DateTime utcNow)
    {
        var age = utcNow - createdAtUtc;
        if (age < TimeSpan.FromMinutes(1))
        {
            return "щойно";
        }

        if (age < TimeSpan.FromHours(1))
        {
            var minutes = Math.Max(1, (int)Math.Floor(age.TotalMinutes));
            return $"{minutes} хв тому";
        }

        if (age < TimeSpan.FromDays(1))
        {
            var hours = Math.Max(1, (int)Math.Floor(age.TotalHours));
            return $"{hours} год тому";
        }

        if (age < TimeSpan.FromDays(2))
        {
            return "вчора";
        }

        return createdAtUtc.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("uk-UA"));
    }
}


