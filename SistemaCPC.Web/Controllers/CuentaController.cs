using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Web.Patrones.Proxy;

namespace SistemaCPC.Web.Controllers;

public class CuentaController : Controller
{
    private readonly IServicioAutenticacion _autenticacion;

    public CuentaController(IServicioAutenticacion autenticacion)
    {
        _autenticacion = autenticacion;
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

        var resultado = await _autenticacion.IniciarSesion(registro, contrasena);

        if (resultado.Usuario == null)
        {
            ViewData["Error"] = resultado.Error;
            return View();
        }

        var usuario = resultado.Usuario;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Registro),
            new("Nombre", usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol)
        };

        if (usuario.Rol == "Superadministrador")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Directorio"));
        }

        claims.AddRange(ProxyApi.ClaimsDelToken(usuario));

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
