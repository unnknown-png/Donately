using Donately.Application.Common.Results;
using Donately.Application.ViewModels;

namespace Donately.Application.Interfaces;

public interface IRecentDonationsService
{
    Task<Result<RecentDonationsFeedViewModel>> GetLatestAsync(int limit = 6, CancellationToken cancellationToken = default);
}

