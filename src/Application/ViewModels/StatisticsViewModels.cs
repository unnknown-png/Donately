#pragma warning disable IDE0051

namespace Donately.Application.ViewModels;

public sealed class StatisticsDashboardViewModel
{
    public string Title { get; init; } = "Статистика";

    public string Subtitle { get; init; } = string.Empty;

    public string? PageError { get; init; }

    public DateTime GeneratedAt { get; init; }

    public string GeneratedAtLabel { get; init; } = string.Empty;

    public int TotalUsersCount { get; init; }

    public int ActiveFundraisersCount { get; init; }

    public int SupportedFundraisersCount { get; init; }

    public int TotalDonationsCount { get; init; }

    public string TotalRaisedAmountLabel { get; init; } = string.Empty;

    public string MostPopularCurrencyLabel { get; init; } = "—";

    public string MostPopularCategoryLabel { get; init; } = "—";

    public IReadOnlyList<StatisticsMetricCardViewModel> Metrics { get; init; } = [];

    public IReadOnlyList<StatisticsCurrencyBreakdownViewModel> CurrencyBreakdown { get; init; } = [];

    public IReadOnlyList<StatisticsTrendPointViewModel> DonationActivity { get; init; } = [];

    public IReadOnlyList<StatisticsTrendPointViewModel> FundraiserActivity { get; init; } = [];

    public IReadOnlyList<StatisticsBreakdownViewModel> CategoryBreakdown { get; init; } = [];

    public IReadOnlyList<TopFundraiserStatViewModel> TopFundraisers { get; init; } = [];

    public StatisticsInsightViewModel LatestDonation { get; init; } = new();

    public StatisticsInsightViewModel LargestDonation { get; init; } = new();
}

public sealed class StatisticsMetricCardViewModel
{
    public string Title { get; init; } = string.Empty;

    public string ValueLabel { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = "•";

    public string ToneClass { get; init; } = "stats-metric-card--tone-neutral";
}

public sealed class StatisticsCurrencyBreakdownViewModel
{
    public string Currency { get; init; } = string.Empty;

    public string AmountLabel { get; init; } = string.Empty;

    public string DonationCountLabel { get; init; } = string.Empty;
}

public sealed class StatisticsTrendPointViewModel
{
    public string Label { get; init; } = string.Empty;

    public int Value { get; init; }

    public string ValueLabel { get; init; } = string.Empty;

    public int BarHeight { get; init; }
}

public sealed class StatisticsBreakdownViewModel
{
    public string Label { get; init; } = string.Empty;

    public string CountLabel { get; init; } = string.Empty;

    public string ShareLabel { get; init; } = string.Empty;

    public int BarWidth { get; init; }
}

public sealed class TopFundraiserStatViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string CategoryLabel { get; init; } = string.Empty;

    public string CurrentAmountLabel { get; init; } = string.Empty;

    public string GoalAmountLabel { get; init; } = string.Empty;

    public string DonationCountLabel { get; init; } = string.Empty;

    public int ProgressPercent { get; init; }

    public string StatusLabel { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsCompleted { get; init; }
}

public sealed class StatisticsInsightViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ValueLabel { get; init; } = string.Empty;

    public string MetaLabel { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string LinkLabel { get; init; } = "Переглянути збір";

    public bool IsEmpty => string.IsNullOrWhiteSpace(Title);
}

#pragma warning restore IDE0051

