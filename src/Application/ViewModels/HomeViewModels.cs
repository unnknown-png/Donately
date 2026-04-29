namespace Donately.Application.ViewModels;

public sealed class HomeIndexViewModel
{
    public RecentDonationsFeedViewModel RecentDonations { get; init; } = new();
}

public sealed class RecentDonationsFeedViewModel
{
    public IReadOnlyList<RecentDonationCardViewModel> Items { get; init; } = [];

    public string EmptyTitle { get; init; } = "Поки що немає останніх донатів";

    public string EmptyDescription { get; init; } = "Щойно з’явиться новий донат, стрічка оновиться автоматично.";

    public DateTimeOffset RefreshedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class RecentDonationCardViewModel
{
    public Guid Id { get; init; }

    public string DonorName { get; init; } = string.Empty;

    public string DonorInitial { get; init; } = "U";

    public string? DonorProfileImagePath { get; init; }

    public string FundraiserTitle { get; init; } = string.Empty;

    public string FundraiserSlug { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "UAH";

    public string AmountLabel { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public string TimeLabel { get; init; } = string.Empty;

    public bool IsAnonymous { get; init; }
}

