using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Models;

namespace SistemaCPC.Web.Controllers;

public class HomeController : Controller
{
    // Después de iniciar sesión cada rol va a su panel.
    [Authorize]
    public IActionResult Index()
    {
        if (User.IsInRole("Afiliado"))
        {
            return RedirectToAction("Index", "MiCuenta");
        }

        return RedirectToAction("Index", "Afiliados");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
