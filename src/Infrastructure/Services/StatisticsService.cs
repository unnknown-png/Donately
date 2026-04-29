using System.Globalization;
using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public sealed class StatisticsService : IStatisticsService
{
    private static readonly CultureInfo UkrainianCulture = CultureInfo.GetCultureInfo("uk-UA");
    private static readonly string[] CurrencyOrder = ["UAH", "USD", "EUR"];

    private readonly ApplicationDbContext _dbContext;

    public StatisticsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StatisticsDashboardViewModel>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var dayRangeStart = utcNow.Date.AddDays(-6);
        var dayRangeEnd = utcNow.Date.AddDays(1);

        var totalUsers = await _dbContext.Users.AsNoTracking().CountAsync(cancellationToken);
        var verifiedUsers = await _dbContext.Users.AsNoTracking().CountAsync(x => x.IsVerified, cancellationToken);
        var totalFundraisers = await _dbContext.Fundraisers.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
        var activeFundraisers = await _dbContext.Fundraisers.AsNoTracking().CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);
        var urgentFundraisers = await _dbContext.Fundraisers.AsNoTracking().CountAsync(x => !x.IsDeleted && x.IsActive && x.IsUrgent, cancellationToken);
        var totalDonations = await _dbContext.Donations.AsNoTracking().CountAsync(cancellationToken);
        var completedDonationsQuery = _dbContext.Donations
            .AsNoTracking()
            .Where(x => x.Status == DonationStatus.Completed);

        var completedDonationCount = await completedDonationsQuery.CountAsync(cancellationToken);
        var supportedFundraisersCount = await completedDonationsQuery
            .Select(x => x.FundraiserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var donorCount = await completedDonationsQuery
            .Where(x => x.DonorId.HasValue)
            .Select(x => x.DonorId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        var anonymousDonationCount = await completedDonationsQuery.CountAsync(x => x.Anonymous, cancellationToken);

        var completedDonations = await completedDonationsQuery
            .Select(x => new
            {
                x.Amount,
                x.Currency,
                x.Anonymous,
                x.CreatedAt,
                x.FundraiserId,
                FundraiserTitle = x.Fundraiser.Title,
                FundraiserSlug = x.Fundraiser.Slug,
                FundraiserCategory = x.Fundraiser.Category,
                DonorFullName = x.Donor != null ? x.Donor.FullName : null,
                DonorUserName = x.Donor != null ? x.Donor.UserName : null
            })
            .ToListAsync(cancellationToken);

        var currencyBreakdown = BuildCurrencyBreakdown(completedDonations);
        var mostPopularCurrencyLabel = currencyBreakdown
            .FirstOrDefault()
            ?.Currency ?? "—";

        var categoryBreakdown = BuildCategoryBreakdown(completedDonations);
        var mostPopularCategoryLabel = categoryBreakdown
            .OrderByDescending(x => x.BarWidth)
            .Select(x => x.Label)
            .FirstOrDefault() ?? "—";

        var dailyDonationActivity = BuildDailyDonationActivity(completedDonations, dayRangeStart, dayRangeEnd);
        var dailyFundraiserActivity = await BuildDailyFundraiserActivity(dayRangeStart, dayRangeEnd, cancellationToken);
        var topFundraisers = await BuildTopFundraisersAsync(cancellationToken);
        var latestDonation = BuildLatestDonation(completedDonations, utcNow);
        var largestDonation = BuildLargestDonation(completedDonations, utcNow);

        var metrics = BuildMetrics(
            totalUsers,
            verifiedUsers,
            totalFundraisers,
            activeFundraisers,
            urgentFundraisers,
            totalDonations,
            completedDonationCount,
            supportedFundraisersCount,
            donorCount,
            anonymousDonationCount);

        return new StatisticsDashboardViewModel
        {
            Subtitle = "Актуальна картина розвитку застосунку: збори, донати, користувачі та найцікавіші тренди за останній час.",
            GeneratedAt = utcNow,
            GeneratedAtLabel = FormatKyivDateTimeLabel(utcNow),
            TotalUsersCount = totalUsers,
            ActiveFundraisersCount = activeFundraisers,
            SupportedFundraisersCount = supportedFundraisersCount,
            TotalDonationsCount = totalDonations,
            MostPopularCurrencyLabel = mostPopularCurrencyLabel,
            MostPopularCategoryLabel = mostPopularCategoryLabel,
            Metrics = metrics,
            CurrencyBreakdown = currencyBreakdown,
            DonationActivity = dailyDonationActivity,
            FundraiserActivity = dailyFundraiserActivity,
            CategoryBreakdown = categoryBreakdown,
            TopFundraisers = topFundraisers,
            LatestDonation = latestDonation,
            LargestDonation = largestDonation
        };
    }

    private static IReadOnlyList<StatisticsMetricCardViewModel> BuildMetrics(
        int totalUsers,
        int verifiedUsers,
        int totalFundraisers,
        int activeFundraisers,
        int urgentFundraisers,
        int totalDonations,
        int completedDonationCount,
        int supportedFundraisersCount,
        int donorCount,
        int anonymousDonationCount)
    {
        var verifiedShare = totalUsers == 0 ? 0 : Math.Round((decimal)verifiedUsers / totalUsers * 100m, 0);
        var anonymousShare = completedDonationCount == 0 ? 0 : Math.Round((decimal)anonymousDonationCount / completedDonationCount * 100m, 0);

        return [
            new StatisticsMetricCardViewModel
            {
                Title = "Зареєстровані користувачі",
                ValueLabel = totalUsers.ToString("N0", UkrainianCulture),
                Description = $"Верифіковано {verifiedUsers.ToString("N0", UkrainianCulture)} користувачів ({verifiedShare}%)",
                Icon = "👥",
                ToneClass = "stats-metric-card--tone-blue"
            },
            new StatisticsMetricCardViewModel
            {
                Title = "Збори на платформі",
                ValueLabel = totalFundraisers.ToString("N0", UkrainianCulture),
                Description = $"Активних зараз {activeFundraisers.ToString("N0", UkrainianCulture)}, термінових {urgentFundraisers.ToString("N0", UkrainianCulture)}",
                Icon = "🎯",
                ToneClass = "stats-metric-card--tone-amber"
            },
            new StatisticsMetricCardViewModel
            {
                Title = "Донати в системі",
                ValueLabel = totalDonations.ToString("N0", UkrainianCulture),
                Description = $"Підтверджених донатів {completedDonationCount.ToString("N0", UkrainianCulture)}, анонімних {anonymousDonationCount.ToString("N0", UkrainianCulture)} ({anonymousShare}%)",
                Icon = "💸",
                ToneClass = "stats-metric-card--tone-cyan"
            },
            new StatisticsMetricCardViewModel
            {
                Title = "Підтримано зборів",
                ValueLabel = supportedFundraisersCount.ToString("N0", UkrainianCulture),
                Description = $"Донати залишили слід у {supportedFundraisersCount.ToString("N0", UkrainianCulture)} різних зборах",
                Icon = "🤝",
                ToneClass = "stats-metric-card--tone-emerald"
            },
            new StatisticsMetricCardViewModel
            {
                Title = "Унікальних донорів",
                ValueLabel = donorCount.ToString("N0", UkrainianCulture),
                Description = "Користувачі, які вже підтримали хоча б один збір",
                Icon = "🛡️",
                ToneClass = "stats-metric-card--tone-violet"
            }
        ];
    }

    private static IReadOnlyList<StatisticsCurrencyBreakdownViewModel> BuildCurrencyBreakdown(IEnumerable<dynamic> donations)
    {
        var items = donations
            .GroupBy(x => NormalizeCurrency((string)x.Currency))
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(x => (decimal)x.Amount),
                Count = group.Count()
            })
            .ToDictionary(x => x.Currency, x => x);

        return items.Values
            .OrderByDescending(x => x.Count)
            .ThenBy(x => Array.IndexOf(CurrencyOrder, x.Currency))
            .Select(currency => new StatisticsCurrencyBreakdownViewModel
            {
                Currency = currency.Currency,
                AmountLabel = FormatAmount(currency.Amount, currency.Currency),
                DonationCountLabel = currency.Count.ToString("N0", UkrainianCulture) + " донатів"
            })
            .ToList();
    }

    private static IReadOnlyList<StatisticsTrendPointViewModel> BuildDailyDonationActivity(IEnumerable<dynamic> donations, DateTime startDate, DateTime endDate)
    {
        var grouped = donations
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate)
            .GroupBy(x => ((DateTime)x.CreatedAt).Date)
            .ToDictionary(x => x.Key, x => x.Count());

        return BuildDailySeries(startDate, endDate, grouped, "донатів");
    }

    private async Task<IReadOnlyList<StatisticsTrendPointViewModel>> BuildDailyFundraiserActivity(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var fundraisers = await _dbContext.Fundraisers
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.CreatedAt >= startDate && x.CreatedAt < endDate)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var grouped = fundraisers
            .GroupBy(x => x.Date)
            .ToDictionary(x => x.Key, x => x.Count());

        return BuildDailySeries(startDate, endDate, grouped, "зборів");
    }

    private static IReadOnlyList<StatisticsTrendPointViewModel> BuildDailySeries(DateTime startDate, DateTime endDate, IReadOnlyDictionary<DateTime, int> grouped, string suffix)
    {
        var days = Enumerable.Range(0, (endDate.Date - startDate.Date).Days)
            .Select(offset => startDate.Date.AddDays(offset))
            .Select(day =>
            {
                var value = grouped.TryGetValue(day, out var count) ? count : 0;
                return new StatisticsTrendPointViewModel
                {
                    Label = day.ToString("dd.MM", UkrainianCulture),
                    Value = value,
                    ValueLabel = value.ToString("N0", UkrainianCulture) + $" {suffix}",
                    BarHeight = 0
                };
            })
            .ToList();

        var maxValue = days.Count == 0 ? 0 : days.Max(x => x.Value);

        for (var index = 0; index < days.Count; index++)
        {
            var point = days[index];
            days[index] = new StatisticsTrendPointViewModel
            {
                Label = point.Label,
                Value = point.Value,
                ValueLabel = point.ValueLabel,
                BarHeight = CalculateBarHeight(point.Value, maxValue)
            };
        }

        return days;
    }

    private static IReadOnlyList<StatisticsBreakdownViewModel> BuildCategoryBreakdown(IEnumerable<dynamic> donations)
    {
        var grouped = donations
            .GroupBy(x => ((FundraiserCategory)x.FundraiserCategory))
            .Select(group => new
            {
                Category = group.Key,
                Count = group.Count()
            })
            .ToDictionary(x => x.Category, x => x.Count);

        var total = grouped.Values.Sum();

        return Enum.GetValues<FundraiserCategory>()
            .Select(category =>
            {
                var count = grouped.TryGetValue(category, out var value) ? value : 0;
                var share = total == 0 ? 0 : Math.Round((decimal)count / total * 100m, 1);

                return new StatisticsBreakdownViewModel
                {
                    Label = ToCategoryLabel(category),
                    CountLabel = count.ToString("N0", UkrainianCulture) + " донатів",
                    ShareLabel = $"{share}%",
                    BarWidth = (int)Math.Round(share, MidpointRounding.AwayFromZero)
                };
            })
            .OrderByDescending(x => x.BarWidth)
            .ToList();
    }

    private async Task<IReadOnlyList<TopFundraiserStatViewModel>> BuildTopFundraisersAsync(CancellationToken cancellationToken)
    {
        var topFundraisers = await _dbContext.Fundraisers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new
            {
                x.Title,
                x.Slug,
                x.Category,
                x.GoalAmount,
                x.CurrentAmount,
                x.Currency,
                x.IsActive,
                DonationCount = x.Donations.Count(d => d.Status == DonationStatus.Completed),
                RaisedAmount = x.Donations.Where(d => d.Status == DonationStatus.Completed).Sum(d => (decimal?)d.Amount) ?? 0m
            })
            .OrderByDescending(x => x.RaisedAmount)
            .ThenByDescending(x => x.DonationCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        return topFundraisers
            .Select(fundraiser =>
            {
                var progressPercent = fundraiser.GoalAmount <= 0
                    ? 0
                    : (int)Math.Min(100m, Math.Round(fundraiser.CurrentAmount / fundraiser.GoalAmount * 100m, 0));
                var completed = fundraiser.CurrentAmount >= fundraiser.GoalAmount;

                return new TopFundraiserStatViewModel
                {
                    Title = fundraiser.Title,
                    Slug = fundraiser.Slug,
                    CategoryLabel = ToCategoryLabel(fundraiser.Category),
                    CurrentAmountLabel = FormatAmount(fundraiser.CurrentAmount, fundraiser.Currency),
                    GoalAmountLabel = FormatAmount(fundraiser.GoalAmount, fundraiser.Currency),
                    DonationCountLabel = fundraiser.DonationCount.ToString("N0", UkrainianCulture) + " донатів",
                    ProgressPercent = progressPercent,
                    StatusLabel = completed ? "Мету досягнуто" : fundraiser.IsActive ? "Активний" : "Неактивний",
                    IsActive = fundraiser.IsActive,
                    IsCompleted = completed
                };
            })
            .ToList();
    }

    private static StatisticsInsightViewModel BuildLatestDonation(IEnumerable<dynamic> donations, DateTime utcNow)
    {
        var latest = donations
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            return new StatisticsInsightViewModel
            {
                Label = "Остання активність",
                Title = string.Empty,
                Description = "Поки що немає підтверджених донатів.",
                ValueLabel = string.Empty,
                MetaLabel = string.Empty,
                LinkLabel = string.Empty
            };
        }

        var donorName = ResolveDonorName((string?)latest.DonorFullName, (string?)latest.DonorUserName, (bool)latest.Anonymous);

        return new StatisticsInsightViewModel
        {
            Label = "Останній донат",
            Title = (string)latest.FundraiserTitle,
            Description = donorName,
            ValueLabel = FormatAmount((decimal)latest.Amount, (string)latest.Currency),
            MetaLabel = BuildRelativeTimeLabel((DateTime)latest.CreatedAt, utcNow),
            Slug = (string)latest.FundraiserSlug,
            LinkLabel = "Перейти до збору"
        };
    }

    private static StatisticsInsightViewModel BuildLargestDonation(IEnumerable<dynamic> donations, DateTime utcNow)
    {
        var largest = donations
            .OrderByDescending(x => x.Amount)
            .FirstOrDefault();

        if (largest is null)
        {
            return new StatisticsInsightViewModel
            {
                Label = "Найбільший донат",
                Title = string.Empty,
                Description = "Поки що немає даних для цього блоку.",
                ValueLabel = string.Empty,
                MetaLabel = string.Empty,
                LinkLabel = string.Empty
            };
        }

        var donorName = ResolveDonorName((string?)largest.DonorFullName, (string?)largest.DonorUserName, (bool)largest.Anonymous);

        return new StatisticsInsightViewModel
        {
            Label = "Найбільший донат",
            Title = (string)largest.FundraiserTitle,
            Description = donorName,
            ValueLabel = FormatAmount((decimal)largest.Amount, (string)largest.Currency),
            MetaLabel = BuildRelativeTimeLabel((DateTime)largest.CreatedAt, utcNow),
            Slug = (string)largest.FundraiserSlug,
            LinkLabel = "Відкрити збір"
        };
    }

    private static int CalculateBarHeight(int value, int maxValue)
    {
        if (value <= 0 || maxValue <= 0)
        {
            return 0;
        }

        var height = (int)Math.Round((decimal)value / maxValue * 100m, 0, MidpointRounding.AwayFromZero);
        return Math.Max(12, height);
    }

    private static string FormatAmount(decimal amount, string currency)
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        return $"{amount.ToString("0.##", UkrainianCulture)} {normalizedCurrency}";
    }

    private static string ResolveDonorName(string? fullName, string? userName, bool anonymous)
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

        return createdAtUtc.ToString("dd.MM.yyyy", UkrainianCulture);
    }

    private static string ToCategoryLabel(FundraiserCategory category)
    {
        return category switch
        {
            FundraiserCategory.Medicine => "Медицина",
            FundraiserCategory.MilitarySupport => "Допомога ЗСУ",
            _ => "Підтримка"
        };
    }

    private static string NormalizeCurrency(string currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? "UAH"
            : currency.Trim().ToUpperInvariant();
    }

    private static string FormatKyivDateTimeLabel(DateTime utcTime)
    {
        var timeZone = GetKyivTimeZone();
        var kyivTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), timeZone);
        return kyivTime.ToString("dd.MM.yyyy HH:mm", UkrainianCulture);
    }

    private static TimeZoneInfo GetKyivTimeZone()
    {
        var timeZoneIds = OperatingSystem.IsWindows()
            ? new[] { "FLE Standard Time", "Europe/Kyiv" }
            : new[] { "Europe/Kyiv", "FLE Standard Time" };

        var timeZone = TimeZoneInfo.GetSystemTimeZones()
            .FirstOrDefault(x => timeZoneIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase));

        return timeZone ?? TimeZoneInfo.Local;
    }
}




