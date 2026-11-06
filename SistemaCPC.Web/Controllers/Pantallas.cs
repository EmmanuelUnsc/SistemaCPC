using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistemaCPC.Web.Controllers;

// Sprint 1: pantallas de los casos de uso según los mockups de Balsamiq.
// Solo muestran datos de ejemplo; sus botones todavía no hacen nada.

// CU-06 Gestionar afiliados
[Authorize(Roles = "Directorio")]
public class AfiliadosController : Controller
{
    public IActionResult Index() => View();
}

// CU-05 Gestionar pagos
[Authorize(Roles = "Directorio")]
public class PagosController : Controller
{
    public IActionResult Index() => View();
}

// CU-07 Gestionar defensas: registrar la defensa y pagarla al tribunal
[Authorize(Roles = "Directorio")]
public class DefensasController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Pago() => View();
}

// CU-09 Generar reportes: defensas del período, afiliados por estado y matriz de afiliados
[Authorize(Roles = "Directorio")]
public class ReportesController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Estados() => View();
    public IActionResult Matriz() => View();
}

// CU-13 Gestionar avisos
[Authorize(Roles = "Directorio")]
public class AvisosController : Controller
{
    public IActionResult Index() => View();
}

// CU-10 Registrar ajuste (solo el superadministrador)
[Authorize(Roles = "Superadministrador")]
public class AjustesController : Controller
{
    public IActionResult Index() => View();
}

// CU-02 Consultar estado de cuenta, CU-03 Consultar saldo de tribunal y CU-04 Pagar aportes (simulado)
[Authorize(Roles = "Afiliado")]
public class MiCuentaController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Defensas() => View();
    public IActionResult Pagar() => View();
}
