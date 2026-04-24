using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public abstract class BaseController : Controller
{
    protected Guid? CurrentUserId
    {
        get
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}

