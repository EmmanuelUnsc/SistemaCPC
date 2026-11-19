using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Directorio")]
public class DefensasController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Pago() => View();
}

[Authorize(Roles = "Directorio")]
public class ReportesController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Estados() => View();
    public IActionResult Matriz() => View();
}

[Authorize(Roles = "Directorio")]
public class AvisosController : Controller
{
    public IActionResult Index() => View();
}
