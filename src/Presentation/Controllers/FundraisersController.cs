using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public class FundraisersController : BaseController
{
    private readonly IFundraiserService _fundraiserService;

    public FundraisersController(IFundraiserService fundraiserService)
    {
        _fundraiserService = fundraiserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _fundraiserService.GetActualFundraisersAsync(cancellationToken);
        if (result.IsFailure)
        {
            TempData["FundraiserError"] = result.Error.Message;
            return RedirectToAction("Index", "Home");
        }

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var accessResult = await _fundraiserService.GetCreateAccessStateAsync(CurrentUserId, cancellationToken);
        if (accessResult.IsFailure)
        {
            TempData["FundraiserError"] = accessResult.Error.Message;
            return RedirectToAction("Index", "Home");
        }

        if (!accessResult.Value.IsVerified)
        {
            return View("CreateAccessWarning", accessResult.Value);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return View("CreateAccessWarning", accessResult.Value);
        }

        var createModelResult = await _fundraiserService.GetCreateModelAsync(userId, cancellationToken);
        if (createModelResult.IsFailure)
        {
            TempData["FundraiserError"] = createModelResult.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(createModelResult.Value);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFundraiserViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Create), "Fundraisers") });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var attachments = new List<UploadFileRequest>();

        foreach (var file in model.Attachments.Where(file => file.Length > 0))
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);

            attachments.Add(new UploadFileRequest
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Content = stream.ToArray()
            });
        }

        UploadFileRequest? coverImage = null;
        if (model.CoverImage is not null && model.CoverImage.Length > 0)
        {
            await using var coverStream = new MemoryStream();
            await model.CoverImage.CopyToAsync(coverStream, cancellationToken);

            coverImage = new UploadFileRequest
            {
                FileName = model.CoverImage.FileName,
                ContentType = model.CoverImage.ContentType,
                Content = coverStream.ToArray()
            };
        }

        var result = await _fundraiserService.CreateAsync(new CreateFundraiserRequest
        {
            UserId = userId,
            Title = model.Title,
            ShortDescription = model.ShortDescription,
            Description = model.Description,
            Category = model.Category,
            GoalAmount = model.GoalAmount,
            Currency = model.Currency,
            IsUrgent = model.IsUrgent,
            CoverImage = coverImage,
            Attachments = attachments
        }, cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["FundraiserSuccess"] = "Збір успішно розміщено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var result = await _fundraiserService.GetDetailsAsync(slug, cancellationToken);
        if (result.IsFailure)
        {
            TempData["FundraiserError"] = result.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }
}

