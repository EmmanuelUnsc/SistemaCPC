using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Models;
using SistemaCPC.Web.Servicios;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Afiliado")]
public class MiCuentaController : Controller
{
    private readonly ProxyApi _api;

    public MiCuentaController(ProxyApi api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var estado = await _api.Obtener<EstadoCuenta>($"api/estado-cuenta/{User.Identity!.Name}");
        ViewData["Error"] = estado.Error;
        return View(estado.Datos);
    }

    public async Task<IActionResult> Pagar()
    {
        var estado = await _api.Obtener<EstadoCuenta>($"api/estado-cuenta/{User.Identity!.Name}");
        ViewData["Error"] = estado.Error;
        return View(estado.Datos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pagar(int mesesCubiertos)
    {
        var pago = await _api.Enviar<PagoRegistrado>(HttpMethod.Post, "api/pagos/plataforma", new { mesesCubiertos });

        if (!pago.Correcta)
        {
            TempData["Error"] = pago.Error;
            return RedirectToAction(nameof(Pagar));
        }

        TempData["Mensaje"] = $"Pago registrado: recibo {pago.Datos!.NumeroRecibo} por {Formato.Bs(pago.Datos.Monto)} " +
                              $"({pago.Datos.MesesCubiertos} meses).";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Defensas()
    {
        if (!User.HasClaim("EsTribunal", "true"))
        {
            TempData["Error"] = "Mis defensas es solo para los afiliados habilitados como tribunal.";
            return RedirectToAction(nameof(Index));
        }

        return View();
    }
}
