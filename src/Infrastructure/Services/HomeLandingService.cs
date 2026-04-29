using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using System.Globalization;

namespace Donately.Infrastructure.Services;

public sealed class HomeLandingService : IHomeLandingService
{
    private const int ActiveFundraisersLimit = 3;
    private const int RecentDonationsLimit = 6;
    private static readonly CultureInfo UkrainianCulture = CultureInfo.GetCultureInfo("uk-UA");

    private readonly IRecentDonationsService _recentDonationsService;
    private readonly IFundraiserService _fundraiserService;
    private readonly IStatisticsService _statisticsService;

    public HomeLandingService(
        IRecentDonationsService recentDonationsService,
        IFundraiserService fundraiserService,
        IStatisticsService statisticsService)
    {
        _recentDonationsService = recentDonationsService;
        _fundraiserService = fundraiserService;
        _statisticsService = statisticsService;
    }

    public async Task<Result<HomeIndexViewModel>> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        var recentDonationsResult = await _recentDonationsService.GetLatestAsync(RecentDonationsLimit, cancellationToken);

        var activeFundraisersResult = await _fundraiserService.GetActualFundraisersAsync(new FundraisersFilterRequest(), cancellationToken);

        var statisticsResult = await _statisticsService.GetDashboardAsync(cancellationToken);

        var recentDonations = recentDonationsResult.IsSuccess
            ? recentDonationsResult.Value
            : new RecentDonationsFeedViewModel
            {
                EmptyTitle = "Стрічка тимчасово недоступна",
                EmptyDescription = recentDonationsResult.Error.Message
            };

        var activeFundraisers = activeFundraisersResult.IsSuccess
            ? activeFundraisersResult.Value.Items
                .Take(ActiveFundraisersLimit)
                .Select(item => new HomeFundraiserCardViewModel
                {
                    Title = item.Title,
                    Slug = item.Slug,
                    ShortDescription = item.ShortDescription,
                    CategoryLabel = item.CategoryLabel,
                    CurrentAmountLabel = $"{item.CurrentAmount.ToString("N0", UkrainianCulture)} {item.Currency}",
                    GoalAmountLabel = $"{item.GoalAmount.ToString("N0", UkrainianCulture)} {item.Currency}",
                    ProgressPercent = CalculateProgress(item.CurrentAmount, item.GoalAmount),
                    StatusLabel = BuildStatusLabel(item.IsUrgent, item.IsNew),
                    IsUrgent = item.IsUrgent,
                    IsNew = item.IsNew,
                    CoverImageUrl = item.CoverImageUrl
                })
                .ToList()
            : [];

        var statistics = statisticsResult.IsSuccess
            ? statisticsResult.Value
            : new StatisticsDashboardViewModel
            {
                TotalUsersCount = 0,
                ActiveFundraisersCount = 0,
                SupportedFundraisersCount = 0
            };

        return new HomeIndexViewModel
        {
            RecentDonations = recentDonations,
            ActiveFundraisers = activeFundraisers,
            Achievements = BuildAchievements(statistics),
            FaqItems = BuildFaqItems()
        };
    }

    private static IReadOnlyList<HomeAchievementCardViewModel> BuildAchievements(StatisticsDashboardViewModel statistics)
    {
        var totalRaisedAmount = statistics.CurrencyBreakdown
            .Select(x => x.AmountLabel)
            .ToList();

        return [
            new HomeAchievementCardViewModel
            {
                Title = "Зареєстрованих користувачів",
                ValueLabel = statistics.TotalUsersCount.ToString("N0", UkrainianCulture),
                Hint = "Люди, які вже приєдналися до Donately.",
                ToneClass = "achievement-card--tone-blue"
            },
            new HomeAchievementCardViewModel
            {
                Title = "Активних зборів",
                ValueLabel = statistics.ActiveFundraisersCount.ToString("N0", UkrainianCulture),
                Hint = "Збори, які зараз відкриті для допомоги.",
                ToneClass = "achievement-card--tone-emerald"
            },
            new HomeAchievementCardViewModel
            {
                Title = "Підтриманих зборів",
                ValueLabel = statistics.SupportedFundraisersCount.ToString("N0", UkrainianCulture),
                Hint = "Скільки різних зборів уже отримали донати.",
                ToneClass = "achievement-card--tone-violet"
            }
        ];
    }

    private static IReadOnlyList<HomeFaqItemViewModel> BuildFaqItems()
    {
        return [
            new HomeFaqItemViewModel
            {
                Question = "Як зробити донат?",
                Answer = "Відкрий сторінку потрібного збору, обери суму, за потреби додай коментар і підтвердь оплату через платіжну форму."
            },
            new HomeFaqItemViewModel
            {
                Question = "Чи можна донатити анонімно?",
                Answer = "Так, у формі донату можна обрати анонімний режим — тоді ім’я не буде відображатися у стрічці останніх донатів."
            },
            new HomeFaqItemViewModel
            {
                Question = "Як створити власний збір?",
                Answer = "Спершу увійди в акаунт, пройди верифікацію профілю, а потім натисни «Створити збір» і заповни всі необхідні поля."
            },
            new HomeFaqItemViewModel
            {
                Question = "Де подивитися результати платформи?",
                Answer = "На сторінці «Досягнення» ти знайдеш статистику по користувачах, зборах, донатах та корисні інсайти по активності."
            }
        ];
    }

    private static int CalculateProgress(decimal currentAmount, decimal goalAmount)
    {
        if (goalAmount <= 0)
        {
            return 0;
        }

        return (int)Math.Min(100m, Math.Round(currentAmount / goalAmount * 100m, 0));
    }

    private static string BuildStatusLabel(bool isUrgent, bool isNew)
    {
        if (isUrgent && isNew)
        {
            return "Новий і терміновий";
        }

        if (isUrgent)
        {
            return "Терміновий";
        }

        return isNew ? "Новий" : "Активний";
    }
}



