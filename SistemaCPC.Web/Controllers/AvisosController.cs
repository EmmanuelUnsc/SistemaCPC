using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Directorio")]
public class AvisosController : Controller
{
    public IActionResult Index() => View();
}
