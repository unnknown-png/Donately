using Donately.Application.Common.Results;
using Donately.Application.ViewModels;

namespace Donately.Application.Interfaces;

public interface IHomeLandingService
{
    Task<Result<HomeIndexViewModel>> GetHomeAsync(CancellationToken cancellationToken = default);
}

