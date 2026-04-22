using Microsoft.AspNetCore.Mvc;

namespace Donately.Controllers;

public class FundraisersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

