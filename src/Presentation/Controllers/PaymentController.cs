using Donately.Application.Interfaces;
using Donately.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public class PaymentController : BaseController
{
    private readonly IDonationPaymentService _donationPaymentService;

    public PaymentController(IDonationPaymentService donationPaymentService)
    {
        _donationPaymentService = donationPaymentService;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Support(StartDonationPaymentViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction("Login", "Auth", new
            {
                returnUrl = Url.Action("Details", "Fundraisers", new { slug = model.FundraiserSlug })
            });
        }

        if (!ModelState.IsValid)
        {
            TempData["FundraiserError"] = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?? "Некоректні дані донату.";

            return RedirectToAction("Details", "Fundraisers", new { slug = model.FundraiserSlug });
        }

        var checkoutResult = await _donationPaymentService.CreateCheckoutAsync(new CreateDonationPaymentRequest
        {
            UserId = userId,
            FundraiserId = model.FundraiserId,
            FundraiserSlug = model.FundraiserSlug,
            Amount = model.Amount,
            Anonymous = model.Anonymous,
            Note = model.Note
        }, cancellationToken);

        if (checkoutResult.IsFailure)
        {
            TempData["FundraiserError"] = checkoutResult.Error.Message;
            return RedirectToAction("Details", "Fundraisers", new { slug = model.FundraiserSlug });
        }

        return View("LiqPayCheckout", checkoutResult.Value);
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> LiqPayCallback([FromForm] string? data, [FromForm] string? signature, CancellationToken cancellationToken)
    {
        var callbackResult = await _donationPaymentService.HandleLiqPayCallbackAsync(new LiqPayCallbackRequest
        {
            Data = data ?? string.Empty,
            Signature = signature ?? string.Empty
        }, cancellationToken);

        return callbackResult.IsFailure
            ? BadRequest(callbackResult.Error.Message)
            : Content("ok", "text/plain");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult LiqPayCallback()
    {
        return Content("ok", "text/plain");
    }
}

