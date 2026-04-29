namespace Donately.Application.ViewModels;

public sealed class HomeIndexViewModel
{
    public RecentDonationsFeedViewModel RecentDonations { get; init; } = new();

    public IReadOnlyList<HomeFundraiserCardViewModel> ActiveFundraisers { get; init; } = [];

    public IReadOnlyList<HomeAchievementCardViewModel> Achievements { get; init; } = [];

    public IReadOnlyList<HomeFaqItemViewModel> FaqItems { get; init; } = [];

    public string ActiveFundraisersTitle { get; init; } = "Активні збори";

    public string ActiveFundraisersSubtitle { get; init; } = "Обирай збір і долучайся до допомоги вже зараз.";

    public string AchievementsTitle { get; init; } = "Досягнення";

    public string AchievementsSubtitle { get; init; } = "Коротко про головні результати застосунку.";

    public string FaqTitle { get; init; } = "Питання та відповіді";

    public string FaqSubtitle { get; init; } = "Найшвидший спосіб зрозуміти, як користуватись Donately.";
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

    public string AmountLabel { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public string TimeLabel { get; init; } = string.Empty;
}

public sealed class HomeFundraiserCardViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CategoryLabel { get; init; } = string.Empty;

    public string CurrentAmountLabel { get; init; } = string.Empty;

    public string GoalAmountLabel { get; init; } = string.Empty;

    public int ProgressPercent { get; init; }

    public string StatusLabel { get; init; } = string.Empty;

    public bool IsUrgent { get; init; }

    public bool IsNew { get; init; }

    public string? CoverImageUrl { get; init; }
}

public sealed class HomeAchievementCardViewModel
{
    public string Title { get; init; } = string.Empty;

    public string ValueLabel { get; init; } = string.Empty;

    public string Hint { get; init; } = string.Empty;

    public string ToneClass { get; init; } = "achievement-card--tone-blue";
}

public sealed class HomeFaqItemViewModel
{
    public string Question { get; init; } = string.Empty;

    public string Answer { get; init; } = string.Empty;
}

