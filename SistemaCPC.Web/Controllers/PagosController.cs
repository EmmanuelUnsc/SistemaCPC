using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Models;
using SistemaCPC.Web.Servicios;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Directorio")]
public class PagosController : Controller
{
    private readonly ProxyApi _api;

    public PagosController(ProxyApi api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? registro)
    {
        var pantalla = new PantallaPagos { Registro = registro };

        if (!string.IsNullOrWhiteSpace(registro))
        {
            var estado = await _api.Obtener<EstadoCuenta>($"api/estado-cuenta/{registro}");
            pantalla.Afiliado = estado.Datos;
            ViewData["Error"] = estado.Error;

            if (estado.Correcta)
            {
                pantalla.Pagos = (await _api.Obtener<List<PagoRegistrado>>($"api/pagos/{registro}")).Datos ?? new();
            }
        }

        return View(pantalla);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(string registro, string numeroRecibo, DateOnly? fecha, int mesesCubiertos)
    {
        var pago = await _api.Enviar<PagoRegistrado>(HttpMethod.Post, "api/pagos/oficina",
            new { registro, numeroRecibo, fecha, mesesCubiertos });

        Avisar(pago.Error, pago.Correcta ? $"Pago {pago.Datos!.NumeroRecibo} registrado por {Formato.Bs(pago.Datos.Monto)}." : null);
        return RedirectToAction(nameof(Index), new { registro });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Corregir(string registro, string numeroRecibo, int mesesCubiertos, decimal monto, string? motivo)
    {
        var pago = await _api.Enviar<PagoRegistrado>(HttpMethod.Put, $"api/pagos/{numeroRecibo}",
            new { mesesCubiertos, monto, motivo = motivo ?? string.Empty });

        Avisar(pago.Error, pago.Correcta ? $"Pago {numeroRecibo} corregido; la corrección quedó en la bitácora." : null);
        return RedirectToAction(nameof(Index), new { registro });
    }

    private void Avisar(string? error, string? mensaje)
    {
        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Mensaje"] = mensaje;
        }
    }
}
