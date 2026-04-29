using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public class StatisticsController : Controller
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetDashboardAsync(cancellationToken);

        if (result.IsFailure)
        {
            return View(new StatisticsDashboardViewModel
            {
                PageError = result.Error.Message
            });
        }

        return View(result.Value);
    }
}


