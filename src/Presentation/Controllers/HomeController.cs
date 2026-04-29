using System.Diagnostics;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public class HomeController : Controller
{
    private const int RecentDonationsLimit = 6;

    private readonly IRecentDonationsService _recentDonationsService;

    public HomeController(IRecentDonationsService recentDonationsService)
    {
        _recentDonationsService = recentDonationsService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await BuildViewModelAsync(cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> RecentDonations(CancellationToken cancellationToken)
    {
        var feed = await LoadRecentDonationsAsync(cancellationToken);
        return PartialView("~/Presentation/Views/Home/_RecentDonationsCards.cshtml", feed);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<HomeIndexViewModel> BuildViewModelAsync(CancellationToken cancellationToken)
    {
        return new HomeIndexViewModel
        {
            RecentDonations = await LoadRecentDonationsAsync(cancellationToken)
        };
    }

    private async Task<RecentDonationsFeedViewModel> LoadRecentDonationsAsync(CancellationToken cancellationToken)
    {
        var result = await _recentDonationsService.GetLatestAsync(RecentDonationsLimit, cancellationToken);

        if (result.IsSuccess)
        {
            return result.Value;
        }

        return new RecentDonationsFeedViewModel
        {
            EmptyTitle = "Стрічка тимчасово недоступна",
            EmptyDescription = result.Error.Message
        };
    }
}
