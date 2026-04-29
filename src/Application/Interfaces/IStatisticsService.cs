using Donately.Application.Common.Results;
namespace Donately.Application.Interfaces;

public interface IStatisticsService
{
    Task<Result<Donately.Application.ViewModels.StatisticsDashboardViewModel>> GetDashboardAsync(CancellationToken cancellationToken = default);
}


