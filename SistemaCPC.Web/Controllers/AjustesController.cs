using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Models;
using SistemaCPC.Web.Patrones.Proxy;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Superadministrador")]
public class AjustesController : Controller
{
    private readonly ProxyApi _api;

    public AjustesController(ProxyApi api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? registro)
    {
        var pantalla = new PantallaAjustes { Registro = registro };

        if (!string.IsNullOrWhiteSpace(registro))
        {
            var estado = await _api.Obtener<EstadoCuenta>($"api/estado-cuenta/{registro}");
            pantalla.Afiliado = estado.Datos;
            ViewData["Error"] = estado.Error;
        }

        pantalla.Bitacora = (await _api.Obtener<List<RegistroBitacora>>("api/ajustes/bitacora")).Datos ?? new();
        return View(pantalla);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(string registro, string tipo, decimal monto, string? motivo)
    {
        var ajuste = await _api.Enviar<AjusteRegistrado>(HttpMethod.Post, "api/ajustes",
            new { registro, tipo, monto, motivo = motivo ?? string.Empty });

        if (ajuste.Correcta)
        {
            TempData["Mensaje"] = $"Ajuste de {Formato.Bs(ajuste.Datos!.Monto)} registrado y anotado en la bitácora.";
        }
        else
        {
            TempData["Error"] = ajuste.Error;
        }

        return RedirectToAction(nameof(Index), new { registro });
    }
}
