using System.ComponentModel.DataAnnotations;
using Donately.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Donately.Application.ViewModels;

public sealed class CreateFundraiserViewModel
{
    [Required(ErrorMessage = "Вкажіть назву збору")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Назва має містити від 5 до 200 символів")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Додайте короткий опис")]
    [StringLength(500, MinimumLength = 20, ErrorMessage = "Короткий опис має містити від 20 до 500 символів")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Додайте детальний опис")]
    [StringLength(8000, MinimumLength = 40, ErrorMessage = "Детальний опис має містити від 40 до 8000 символів")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть категорію")]
    public FundraiserCategory Category { get; set; } = FundraiserCategory.Support;

    [Range(typeof(decimal), "1", "999999999", ErrorMessage = "Ціль збору має бути більшою за 0")]
    public decimal GoalAmount { get; set; }

    [Required(ErrorMessage = "Оберіть валюту")]
    [RegularExpression("^(UAH|USD|EUR)$", ErrorMessage = "Підтримуються лише UAH, USD або EUR")]
    public string Currency { get; set; } = "UAH";

    public bool IsUrgent { get; set; }

    public IFormFile? CoverImage { get; set; }

    public List<IFormFile> Attachments { get; set; } = [];
}

public sealed class FundraiserCreateAccessViewModel
{
    public bool IsAuthenticated { get; init; }

    public bool IsVerified { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

public sealed class FundraisersListViewModel
{
    public IReadOnlyList<FundraiserCardViewModel> Items { get; init; } = [];
}

public sealed class FundraiserCardViewModel
{
    public Guid Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CategoryLabel { get; init; } = string.Empty;

    public bool IsUrgent { get; init; }

    public bool IsNew { get; init; }

    public decimal GoalAmount { get; init; }

    public decimal CurrentAmount { get; init; }

    public string Currency { get; init; } = "UAH";

    public string? CoverImageUrl { get; init; }

    public string AuthorNickName { get; init; } = string.Empty;

    public string AuthorFullName { get; init; } = string.Empty;

    public string? AuthorProfileImagePath { get; init; }
}

public sealed class FundraiserDetailsViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CategoryLabel { get; init; } = string.Empty;

    public bool IsUrgent { get; init; }

    public bool IsNew { get; init; }

    public decimal GoalAmount { get; init; }

    public decimal CurrentAmount { get; init; }

    public string Currency { get; init; } = "UAH";

    public string? CoverImageUrl { get; init; }

    public string AuthorNickName { get; init; } = string.Empty;

    public string AuthorFullName { get; init; } = string.Empty;

    public string? AuthorProfileImagePath { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<FundraiserAttachmentViewModel> Attachments { get; init; } = [];
}

public sealed class FundraiserAttachmentViewModel
{
    public string Url { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }
}

