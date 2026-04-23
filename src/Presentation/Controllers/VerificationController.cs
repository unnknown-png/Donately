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

    [HttpGet]
    public async Task<IActionResult> Phone(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _verificationService.GetPhoneVerificationAsync(userId, cancellationToken);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Phone(PhoneVerificationViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCurrentPhoneStateAsync(userId, model, cancellationToken);
            return View(model);
        }

        var result = await _verificationService.SendPhoneVerificationAsync(
            new StartPhoneVerificationRequest(userId, model.PhoneNumber),
            cancellationToken);

        if (result.IsFailure)
        {
            ViewData["VerificationError"] = result.Error.Message;
            await PopulateCurrentPhoneStateAsync(userId, model, cancellationToken);
            return View(model);
        }

        model.PhoneSent = true;
        model.PhoneConfirmed = false;
        ModelState.Clear();
        ViewData["VerificationSuccess"] = "Тестовий код підготовлено. Для симуляції введи будь-який код нижче.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePhone(CompletePhoneVerificationViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            var currentResult = await _verificationService.GetPhoneVerificationAsync(userId, cancellationToken);

            if (currentResult.IsSuccess)
            {
                var phoneModel = currentResult.Value;
                phoneModel.PhoneSent = true;
                ViewData["VerificationError"] = "Підтвердження телефону не вдалося: код порожній.";
                return View(nameof(Phone), phoneModel);
            }

            TempData["VerificationError"] = "Підтвердження телефону не вдалося: код порожній.";
            return RedirectToAction(nameof(Phone));
        }

        var result = await _verificationService.ConfirmPhoneAsync(
            new ConfirmPhoneVerificationRequest(userId, model.Code),
            cancellationToken);

        if (result.IsFailure)
        {
            var currentResult = await _verificationService.GetPhoneVerificationAsync(userId, cancellationToken);

            if (currentResult.IsSuccess)
            {
                var phoneModel = currentResult.Value;
                phoneModel.PhoneSent = true;
                ViewData["VerificationError"] = result.Error.Message;
                return View(nameof(Phone), phoneModel);
            }

            TempData["VerificationError"] = result.Error.Message;
            return RedirectToAction(nameof(Phone));
        }

        TempData["VerificationSuccess"] = "Номер телефону успішно підтверджено.";
        return RedirectToAction("Index", "Profile");
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

    private async Task PopulateCurrentPhoneStateAsync(
        Guid userId,
        PhoneVerificationViewModel model,
        CancellationToken cancellationToken)
    {
        var currentResult = await _verificationService.GetPhoneVerificationAsync(userId, cancellationToken);

        if (currentResult.IsSuccess)
        {
            model.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? currentResult.Value.PhoneNumber : model.PhoneNumber;
            model.PhoneConfirmed = currentResult.Value.PhoneConfirmed;
            model.VerificationStatusLabel = currentResult.Value.VerificationStatusLabel;
        }
    }
}


