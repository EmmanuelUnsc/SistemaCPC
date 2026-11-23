using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Directorio")]
public class DefensasController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Pago() => View();
}
