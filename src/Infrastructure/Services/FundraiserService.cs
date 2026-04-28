using System.Text;
using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Donately.Domain.Entities;
using Donately.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Services;

public sealed class FundraiserService : IFundraiserService
{
    private static readonly HashSet<string> AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly HashSet<string> AllowedAttachmentExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp"];

    private const int MaxCoverSizeInBytes = 5 * 1024 * 1024;
    private const int MaxAttachmentSizeInBytes = 10 * 1024 * 1024;
    private const int MaxGoalForUah = 10000;
    private const int MaxGoalForUsdEur = 5000;
    private const string AnyCurrency = "ALL";
    private const string FundraiserOwnerType = "Fundraiser";
    private const string CoverPurpose = "CoverImage";
    private const string AttachmentPurpose = "Attachment";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public FundraiserService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<Result<FundraiserCreateAccessViewModel>> GetCreateAccessStateAsync(
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return new FundraiserCreateAccessViewModel
            {
                IsAuthenticated = false,
                IsVerified = false,
                Title = "Створення збору",
                Message = "Щоб створити власний збір, увійди в акаунт і пройди верифікацію профілю."
            };
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return new Error("Fundraiser.UserNotFound", "Користувача не знайдено");
        }

        if (!user.IsVerified)
        {
            return new FundraiserCreateAccessViewModel
            {
                IsAuthenticated = true,
                IsVerified = false,
                Title = "Потрібна верифікація",
                Message = "Для створення збору потрібно завершити верифікацію акаунта в профілі."
            };
        }

        return new FundraiserCreateAccessViewModel
        {
            IsAuthenticated = true,
            IsVerified = true,
            Title = "Створення збору",
            Message = string.Empty
        };
    }

    public async Task<Result<CreateFundraiserViewModel>> GetCreateModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accessStateResult = await GetCreateAccessStateAsync(userId, cancellationToken);
        if (accessStateResult.IsFailure)
        {
            return accessStateResult.Error;
        }

        if (!accessStateResult.Value.IsVerified)
        {
            return new Error("Fundraiser.AccessDenied", "Створення збору доступне лише верифікованим користувачам");
        }

        return new CreateFundraiserViewModel();
    }

    public async Task<Result<CreateFundraiserResult>> CreateAsync(CreateFundraiserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return new Error("Fundraiser.UserNotFound", "Користувача не знайдено");
        }

        if (!user.IsVerified)
        {
            return new Error("Fundraiser.NotVerified", "Спершу потрібно завершити верифікацію акаунта");
        }

        if (request.GoalAmount <= 0)
        {
            return new Error("Fundraiser.InvalidGoal", "Ціль збору має бути більшою за 0");
        }

        var normalizedCurrency = request.Currency.Trim().ToUpperInvariant();
        if (normalizedCurrency is not ("UAH" or "USD" or "EUR"))
        {
            return new Error("Fundraiser.InvalidCurrency", "Підтримуються лише UAH, USD або EUR");
        }

        if (request.CoverImage is not null)
        {
            var coverValidationError = ValidateUpload(
                request.CoverImage,
                AllowedImageExtensions,
                AllowedImageContentTypes,
                MaxCoverSizeInBytes,
                "Fundraiser.Cover");

            if (!coverValidationError.IsNone)
            {
                return coverValidationError;
            }
        }

        foreach (var attachment in request.Attachments)
        {
            var attachmentValidationError = ValidateUpload(
                attachment,
                AllowedAttachmentExtensions,
                null,
                MaxAttachmentSizeInBytes,
                "Fundraiser.Attachment");

            if (!attachmentValidationError.IsNone)
            {
                return attachmentValidationError;
            }
        }

        var fundraiserId = Guid.NewGuid();
        var baseSlug = BuildSlug(request.Title);
        var slug = await EnsureUniqueSlugAsync(baseSlug, cancellationToken);

        var fundraiser = new Fundraiser
        {
            Id = fundraiserId,
            Title = request.Title.Trim(),
            Slug = slug,
            ShortDescription = request.ShortDescription.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            GoalAmount = request.GoalAmount,
            CurrentAmount = 0,
            Currency = normalizedCurrency,
            CreatedById = request.UserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsUrgent = request.IsUrgent
        };

        _dbContext.Fundraisers.Add(fundraiser);

        if (request.CoverImage is not null)
        {
            var savedCover = await SaveUploadAsync(fundraiser.Id, request.CoverImage, "covers", cancellationToken);

            var coverAttachment = new Attachment
            {
                Id = Guid.NewGuid(),
                OwnerType = FundraiserOwnerType,
                OwnerId = fundraiser.Id,
                Url = savedCover.RelativeUrl,
                ContentType = request.CoverImage.ContentType,
                Purpose = CoverPurpose,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Attachments.Add(coverAttachment);
            fundraiser.CoverImageId = coverAttachment.Id;
        }

        foreach (var attachment in request.Attachments)
        {
            var savedAttachment = await SaveUploadAsync(fundraiser.Id, attachment, "attachments", cancellationToken);

            _dbContext.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                OwnerType = FundraiserOwnerType,
                OwnerId = fundraiser.Id,
                Url = savedAttachment.RelativeUrl,
                ContentType = attachment.ContentType,
                Purpose = AttachmentPurpose,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateFundraiserResult
        {
            FundraiserId = fundraiser.Id,
            Slug = fundraiser.Slug
        };
    }

    public async Task<Result<FundraisersListViewModel>> GetActualFundraisersAsync(
        FundraisersFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedFilterResult = NormalizeFilter(request);
        if (normalizedFilterResult.IsFailure)
        {
            return normalizedFilterResult.Error;
        }

        var normalizedFilter = normalizedFilterResult.Value;
        var utcNow = DateTime.UtcNow;
        var newSince = utcNow - TimeSpan.FromHours(24);
        var completedSince = utcNow - TimeSpan.FromSeconds(30);

        var query = _dbContext.Fundraisers
            .AsNoTracking()
            .Include(x => x.CreatedBy)
            .Where(x => x.IsActive && (x.CurrentAmount < x.GoalAmount || (x.UpdatedAt.HasValue && x.UpdatedAt >= completedSince)));

        if (normalizedFilter.Categories.Count > 0)
        {
            query = query.Where(x => normalizedFilter.Categories.Contains(x.Category));
        }

        if (normalizedFilter.Statuses.Count > 0)
        {
            var includeUrgent = normalizedFilter.Statuses.Contains("urgent");
            var includeNew = normalizedFilter.Statuses.Contains("new");

            if (includeUrgent && includeNew)
            {
                query = query.Where(x => x.IsUrgent || x.CreatedAt >= newSince);
            }
            else if (includeUrgent)
            {
                query = query.Where(x => x.IsUrgent);
            }
            else if (includeNew)
            {
                query = query.Where(x => x.CreatedAt >= newSince);
            }
        }

        if (!string.Equals(normalizedFilter.Currency, AnyCurrency, StringComparison.Ordinal))
        {
            query = query.Where(x => x.Currency == normalizedFilter.Currency);
        }

        if (normalizedFilter.GoalAmountMax < normalizedFilter.GoalAmountUpperBound)
        {
            query = query.Where(x => x.GoalAmount <= normalizedFilter.GoalAmountMax);
        }

        var fundraisers = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var coverIds = fundraisers
            .Where(x => x.CoverImageId.HasValue)
            .Select(x => x.CoverImageId!.Value)
            .Distinct()
            .ToList();

        var coverMap = coverIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Attachments
                .AsNoTracking()
                .Where(x => coverIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Url, cancellationToken);

        var cards = fundraisers.Select(fundraiser => new FundraiserCardViewModel
        {
            Id = fundraiser.Id,
            Slug = fundraiser.Slug,
            Title = fundraiser.Title,
            ShortDescription = fundraiser.ShortDescription ?? string.Empty,
            CategoryLabel = ToCategoryLabel(fundraiser.Category),
            IsUrgent = fundraiser.IsUrgent,
            IsNew = utcNow - fundraiser.CreatedAt <= TimeSpan.FromHours(24),
            GoalAmount = fundraiser.GoalAmount,
            CurrentAmount = fundraiser.CurrentAmount,
            Currency = fundraiser.Currency,
            CoverImageUrl = fundraiser.CoverImageId.HasValue && coverMap.TryGetValue(fundraiser.CoverImageId.Value, out var coverUrl)
                ? coverUrl
                : null,
            AuthorNickName = fundraiser.CreatedBy.UserName ?? "Користувач",
            AuthorFullName = string.IsNullOrWhiteSpace(fundraiser.CreatedBy.FullName)
                ? (fundraiser.CreatedBy.UserName ?? "Користувач")
                : fundraiser.CreatedBy.FullName,
            AuthorProfileImagePath = fundraiser.CreatedBy.ProfileImagePath
        }).ToList();

        return new FundraisersListViewModel
        {
            Items = cards,
            Filters = new FundraisersFiltersStateViewModel
            {
                SelectedCategories = normalizedFilter.Categories,
                SelectedStatuses = normalizedFilter.Statuses,
                SelectedCurrency = normalizedFilter.Currency,
                GoalAmountMax = normalizedFilter.GoalAmountMax,
                GoalAmountUpperBound = normalizedFilter.GoalAmountUpperBound
            }
        };
    }

    public async Task<Result<FundraiserDetailsViewModel>> GetDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return new Error("Fundraiser.InvalidSlug", "Некоректне посилання на збір");
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var fundraiser = await _dbContext.Fundraisers
            .AsNoTracking()
            .Include(x => x.CreatedBy)
            .SingleOrDefaultAsync(x => x.IsActive && x.Slug == normalizedSlug, cancellationToken);

        if (fundraiser is null)
        {
            return new Error("Fundraiser.NotFound", "Збір не знайдено");
        }

        var attachments = await _dbContext.Attachments
            .AsNoTracking()
            .Where(x => x.OwnerType == FundraiserOwnerType && x.OwnerId == fundraiser.Id && x.Purpose == AttachmentPurpose)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        string? coverUrl = null;
        if (fundraiser.CoverImageId.HasValue)
        {
            coverUrl = await _dbContext.Attachments
                .AsNoTracking()
                .Where(x => x.Id == fundraiser.CoverImageId.Value)
                .Select(x => x.Url)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new FundraiserDetailsViewModel
        {
            Id = fundraiser.Id,
            Slug = fundraiser.Slug,
            Title = fundraiser.Title,
            Description = fundraiser.Description ?? string.Empty,
            ShortDescription = fundraiser.ShortDescription ?? string.Empty,
            CategoryLabel = ToCategoryLabel(fundraiser.Category),
            IsUrgent = fundraiser.IsUrgent,
            IsNew = DateTime.UtcNow - fundraiser.CreatedAt <= TimeSpan.FromHours(24),
            GoalAmount = fundraiser.GoalAmount,
            CurrentAmount = fundraiser.CurrentAmount,
            Currency = fundraiser.Currency,
            CoverImageUrl = coverUrl,
            AuthorNickName = fundraiser.CreatedBy.UserName ?? "Користувач",
            AuthorFullName = string.IsNullOrWhiteSpace(fundraiser.CreatedBy.FullName)
                ? (fundraiser.CreatedBy.UserName ?? "Користувач")
                : fundraiser.CreatedBy.FullName,
            AuthorProfileImagePath = fundraiser.CreatedBy.ProfileImagePath,
            CreatedAt = fundraiser.CreatedAt,
            Attachments = attachments
                .Select(x => new FundraiserAttachmentViewModel
                {
                    Url = x.Url,
                    FileName = Path.GetFileName(x.Url),
                    ContentType = x.ContentType
                })
                .ToList()
        };
    }

    private static string ToCategoryLabel(FundraiserCategory category)
    {
        return category switch
        {
            FundraiserCategory.Medicine => "Медицина",
            FundraiserCategory.MilitarySupport => "Допомога ЗСУ",
            _ => "Підтримка"
        };
    }

    private static Result<NormalizedFundraiserFilter> NormalizeFilter(FundraisersFilterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var selectedCategories = request.Categories
            .Distinct()
            .ToArray();

        var selectedStatuses = request.Statuses
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x is "urgent" or "new")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var selectedCurrency = string.IsNullOrWhiteSpace(request.Currency)
            ? AnyCurrency
            : request.Currency.Trim().ToUpperInvariant();

        if (selectedCurrency is not (AnyCurrency or "UAH" or "USD" or "EUR"))
        {
            return new Error("Fundraiser.InvalidCurrencyFilter", "Некоректна валюта у фільтрі");
        }

        var goalUpperBound = GetGoalUpperBoundByCurrency(selectedCurrency);
        var selectedGoal = request.GoalAmountMax;
        if (selectedGoal.HasValue && selectedGoal.Value <= 0)
        {
            return new Error("Fundraiser.InvalidGoalFilter", "Сума цілі у фільтрі має бути більшою за 0");
        }

        var normalizedGoal = selectedGoal.HasValue
            ? Math.Min(selectedGoal.Value, goalUpperBound)
            : goalUpperBound;

        return new NormalizedFundraiserFilter(
            selectedCategories,
            selectedStatuses,
            selectedCurrency,
            normalizedGoal,
            goalUpperBound);
    }

    private static int GetGoalUpperBoundByCurrency(string currency)
    {
        return currency is "USD" or "EUR"
            ? MaxGoalForUsdEur
            : MaxGoalForUah;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        var normalizedBaseSlug = string.IsNullOrWhiteSpace(baseSlug) ? "fundraiser" : baseSlug;
        var candidate = normalizedBaseSlug;
        var suffix = 1;

        while (await _dbContext.Fundraisers.AnyAsync(x => x.Slug == candidate, cancellationToken))
        {
            candidate = $"{normalizedBaseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string BuildSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "fundraiser";
        }

        var normalized = title.Trim().ToLowerInvariant();
        var sb = new StringBuilder(normalized.Length);
        var previousIsDash = false;

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                previousIsDash = false;
                continue;
            }

            if (previousIsDash)
            {
                continue;
            }

            sb.Append('-');
            previousIsDash = true;
        }

        var slug = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "fundraiser" : slug;
    }

    private static Error ValidateUpload(
        UploadFileRequest file,
        IReadOnlySet<string> allowedExtensions,
        IReadOnlySet<string>? allowedContentTypes,
        int maxSizeInBytes,
        string errorCodePrefix)
    {
        if (file.Content.Length == 0)
        {
            return new Error($"{errorCodePrefix}.Empty", "Файл порожній");
        }

        if (file.Content.Length > maxSizeInBytes)
        {
            return new Error($"{errorCodePrefix}.TooLarge", "Файл перевищує допустимий розмір");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return new Error($"{errorCodePrefix}.InvalidExtension", "Непідтримуваний формат файлу");
        }

        if (allowedContentTypes is not null && !allowedContentTypes.Contains(file.ContentType))
        {
            return new Error($"{errorCodePrefix}.InvalidContentType", "Непідтримуваний MIME-тип файлу");
        }

        return Error.None;
    }

    private async Task<SavedUpload> SaveUploadAsync(
        Guid fundraiserId,
        UploadFileRequest file,
        string folder,
        CancellationToken cancellationToken)
    {
        var webRootPath = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            throw new InvalidOperationException("WebRootPath не налаштований для збереження файлів.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{fundraiserId:N}_{Guid.NewGuid():N}{extension}";
        var relativeFolder = Path.Combine("uploads", "fundraisers", folder);
        var targetFolder = Path.Combine(webRootPath, relativeFolder);
        Directory.CreateDirectory(targetFolder);

        var absolutePath = Path.Combine(targetFolder, fileName);
        await File.WriteAllBytesAsync(absolutePath, file.Content, cancellationToken);

        var relativeUrl = $"/{relativeFolder.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        return new SavedUpload(relativeUrl);
    }

    private readonly record struct SavedUpload(string RelativeUrl);

    private sealed record NormalizedFundraiserFilter(
        IReadOnlyList<FundraiserCategory> Categories,
        IReadOnlyList<string> Statuses,
        string Currency,
        decimal GoalAmountMax,
        int GoalAmountUpperBound);
}


