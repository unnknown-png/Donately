using System.Security.Claims;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _profileService.GetProfileAsync(userId, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBio(UpdateBioViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Помилка валідації поля 'Про себе'.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _profileService.UpdateBioAsync(
            userId,
            new UpdateBioRequest { Bio = model.Bio },
            cancellationToken);

        TempData[result.IsSuccess ? "ProfileSuccess" : "ProfileError"] = result.IsSuccess
            ? "Поле 'Про себе' успішно оновлено."
            : result.Error.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(UpdateProfileDetailsViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Перевір заповнення полів профілю.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _profileService.UpdateDetailsAsync(
            userId,
            new UpdateProfileDetailsRequest
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth
            },
            cancellationToken);

        TempData[result.IsSuccess ? "ProfileSuccess" : "ProfileError"] = result.IsSuccess
            ? "Профіль успішно оновлено."
            : result.Error.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLocation(UpdateLocationViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Некоректний формат локації.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _profileService.UpdateLocationAsync(
            userId,
            new UpdateLocationRequest { Location = model.Location },
            cancellationToken);

        TempData[result.IsSuccess ? "ProfileSuccess" : "ProfileError"] = result.IsSuccess
            ? "Локацію успішно оновлено."
            : result.Error.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvatar(IFormFile? avatar, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (avatar is null || avatar.Length == 0)
        {
            TempData["ProfileError"] = "Оберіть файл аватара.";
            return RedirectToAction(nameof(Index));
        }

        await using var memoryStream = new MemoryStream();
        await avatar.CopyToAsync(memoryStream, cancellationToken);

        var result = await _profileService.UpdateAvatarAsync(
            userId,
            new UpdateAvatarRequest
            {
                FileName = avatar.FileName,
                ContentType = avatar.ContentType,
                Content = memoryStream.ToArray()
            },
            cancellationToken);

        TempData[result.IsSuccess ? "ProfileSuccess" : "ProfileError"] = result.IsSuccess
            ? "Аватар успішно оновлено."
            : result.Error.Message;

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}

