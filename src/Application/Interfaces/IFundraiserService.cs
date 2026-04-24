using Donately.Application.Common.Results;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;

namespace Donately.Application.Interfaces;

public interface IFundraiserService
{
    Task<Result<FundraiserCreateAccessViewModel>> GetCreateAccessStateAsync(Guid? userId, CancellationToken cancellationToken = default);

    Task<Result<CreateFundraiserViewModel>> GetCreateModelAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<CreateFundraiserResult>> CreateAsync(CreateFundraiserRequest request, CancellationToken cancellationToken = default);

    Task<Result<FundraisersListViewModel>> GetActualFundraisersAsync(CancellationToken cancellationToken = default);

    Task<Result<FundraiserDetailsViewModel>> GetDetailsAsync(string slug, CancellationToken cancellationToken = default);
}

public sealed class CreateFundraiserRequest
{
    public Guid UserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public FundraiserCategory Category { get; init; }

    public decimal GoalAmount { get; init; }

    public string Currency { get; init; } = "UAH";

    public bool IsUrgent { get; init; }

    public UploadFileRequest? CoverImage { get; init; }

    public IReadOnlyList<UploadFileRequest> Attachments { get; init; } = [];
}

public sealed class UploadFileRequest
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();
}

public sealed class CreateFundraiserResult
{
    public Guid FundraiserId { get; init; }

    public string Slug { get; init; } = string.Empty;
}

