using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

