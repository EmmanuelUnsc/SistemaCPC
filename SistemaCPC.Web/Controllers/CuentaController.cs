using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Models;

namespace SistemaCPC.Web.Controllers;

// CU-01 Iniciar sesión: la Web envía el registro y la contraseña a la API y, si son correctos,
// guarda la sesión con el nombre y el rol que devuelve la API.
public class CuentaController : Controller
{
    private readonly IHttpClientFactory _clientes;

    public CuentaController(IHttpClientFactory clientes)
    {
        _clientes = clientes;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string registro, string contrasena)
    {
        ViewData["Registro"] = registro;
        var api = _clientes.CreateClient("Api");

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await api.PostAsJsonAsync("api/auth/login", new { registro, contrasena });
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = "No se pudo conectar con la API. Verifique que SistemaCPC.Api esté en ejecución.";
            return View();
        }

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            ViewData["Error"] = "Registro o contraseña incorrectos.";
            return View();
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            ViewData["Error"] = "Ingrese su registro y su contraseña.";
            return View();
        }

        var usuario = await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new(ClaimTypes.Name, usuario.Registro),
            new("Nombre", usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol)
        };

        // El superadministrador también es miembro del directorio.
        if (usuario.Rol == "Superadministrador")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Directorio"));
        }

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
