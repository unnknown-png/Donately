using System.Security.Claims;
using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

[Authorize]
public class VerificationController : Controller
{
    private readonly IVerificationService _verificationService;

    public VerificationController(IVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Email(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _verificationService.GetEmailVerificationAsync(userId, cancellationToken);

        if (result.IsFailure)
        {
            TempData["VerificationError"] = result.Error.Message;
            return RedirectToAction("Index", "Profile");
        }

        if (TempData["VerificationSuccess"] is string successMessage)
        {
            ViewData["VerificationSuccess"] = successMessage;
        }

        if (TempData["VerificationError"] is string errorMessage)
        {
            ViewData["VerificationError"] = errorMessage;
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Email(EmailVerificationViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCurrentVerificationStateAsync(userId, model, cancellationToken);
            return View(model);
        }

        var result = await _verificationService.SendEmailVerificationAsync(
            new StartEmailVerificationRequest(userId, model.Email),
            cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await PopulateCurrentVerificationStateAsync(userId, model, cancellationToken);
            return View(model);
        }

        model.EmailSent = true;
        model.EmailConfirmed = false;
        model.VerificationStatusLabel = "Не розпочато";
        ModelState.Clear();
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(Guid requestId, string token, CancellationToken cancellationToken)
    {
        var result = await _verificationService.ConfirmEmailAsync(
            new ConfirmEmailVerificationRequest(requestId, token),
            cancellationToken);

        if (result.IsFailure)
        {
            TempData["VerificationError"] = result.Error.Message;
            return RedirectToAction(nameof(Email));
        }

        TempData["VerificationSuccess"] = "Email успішно підтверджено.";

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Profile");
        }

        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "Profile") });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }

    private async Task PopulateCurrentVerificationStateAsync(
        Guid userId,
        EmailVerificationViewModel model,
        CancellationToken cancellationToken)
    {
        var currentResult = await _verificationService.GetEmailVerificationAsync(userId, cancellationToken);

        if (currentResult.IsSuccess)
        {
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? currentResult.Value.Email : model.Email;
            model.EmailConfirmed = currentResult.Value.EmailConfirmed;
            model.VerificationStatusLabel = currentResult.Value.VerificationStatusLabel;
        }
    }
}


