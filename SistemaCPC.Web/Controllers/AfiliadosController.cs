using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Models;
using SistemaCPC.Web.Servicios;

namespace SistemaCPC.Web.Controllers;

[Authorize(Roles = "Directorio")]
public class AfiliadosController : Controller
{
    private readonly ProxyApi _api;

    public AfiliadosController(ProxyApi api)
    {
        _api = api;
    }

    private const int PorPagina = 20;

    public async Task<IActionResult> Index(int pagina = 1, string? registro = null)
    {
        var afiliados = await _api.Obtener<List<AfiliadoDatos>>("api/afiliados");
        ViewData["Error"] = afiliados.Error;
        var todos = afiliados.Datos ?? new();

        var posicion = string.IsNullOrEmpty(registro) ? -1 : todos.FindIndex(a => a.Registro == registro);
        if (posicion >= 0)
        {
            pagina = posicion / PorPagina + 1;
        }

        var paginas = Math.Max(1, (int)Math.Ceiling(todos.Count / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, paginas);
        ViewData["Pagina"] = pagina;
        ViewData["Paginas"] = paginas;
        ViewData["Total"] = todos.Count;
        ViewData["Desde"] = todos.Count == 0 ? 0 : (pagina - 1) * PorPagina + 1;
        return View(todos.Skip((pagina - 1) * PorPagina).Take(PorPagina).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(FormularioAfiliado formulario)
    {
      

        var editar = formulario.Modo == "editar";
        var registro = editar ? formulario.RegistroOriginal : formulario.Registro;

        var datos = new
        {
            Registro = registro,
            formulario.Nombre,
            formulario.Carnet,
            formulario.FechaAfiliacion,
            formulario.FechaInicioCobro,
            formulario.EsTribunal,
            formulario.Contrasena
        };

        var guardado = editar
            ? await _api.Enviar<AfiliadoDatos>(HttpMethod.Put, $"api/afiliados/{registro}", datos)
            : await _api.Enviar<AfiliadoDatos>(HttpMethod.Post, "api/afiliados", datos);

        if (!guardado.Correcta)
        {
            TempData["Error"] = guardado.Error;
            return RedirectToAction(nameof(Index));
        }

        if (formulario.Foto is { Length: > 0 })
        {
            var foto = await _api.EnviarArchivo($"api/afiliados/{registro}/foto", "foto", formulario.Foto);

            if (!foto.Correcta)
            {
                TempData["Error"] = $"Los datos se guardaron, pero la foto no: {foto.Error}";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Mensaje"] = editar
                       ? $"Afiliado {registro} actualizado."
            : $"Afiliado {registro} registrado; ya puede iniciar sesión con su contraseña inicial.";
        return RedirectToAction(nameof(Index), new { registro });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Baja(FormularioAfiliado formulario)
    {
        if (string.IsNullOrWhiteSpace(formulario.Registro))
        {
            TempData["Error"] = "Elija en la lista al afiliado que quiere dar de baja o escriba su número de registro.";
            return RedirectToAction(nameof(Index));
        }

        var baja = await _api.Enviar<AfiliadoDatos>(HttpMethod.Post, $"api/afiliados/{formulario.Registro}/baja",
            new { motivo = formulario.Baja ?? string.Empty, fecha = formulario.FechaBaja });

        if (baja.Correcta)
        {
            TempData["Mensaje"] = $"Baja registrada para {formulario.Registro}: desde esa fecha no se le suman aportes.";
        }
        else
        {
            TempData["Error"] = baja.Error;
        }

        return RedirectToAction(nameof(Index), new { registro = formulario.Registro });
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Foto(string id)
    {
        var foto = await _api.ObtenerArchivo($"api/afiliados/{id}/foto");
        return foto == null ? NotFound() : File(foto.Value.Datos, foto.Value.Tipo);
    }
}
