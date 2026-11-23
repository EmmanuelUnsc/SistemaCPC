using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SistemaCPC.Web.Servicios;

namespace SistemaCPC.Web.Filtros;

public class FiltroErroresApi : IAsyncExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempData;

    public FiltroErroresApi(ITempDataDictionaryFactory tempData)
    {
        _tempData = tempData;
    }

    public async Task OnExceptionAsync(ExceptionContext contexto)
    {
        if (contexto.Exception is SesionVencidaException)
        {
            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _tempData.GetTempData(contexto.HttpContext)["Error"] = "Su sesión se cerró. Vuelva a iniciar sesión.";
            contexto.Result = new RedirectToActionResult("Login", "Cuenta", null);
            contexto.ExceptionHandled = true;
        }
        else if (contexto.Exception is HttpRequestException)
        {
            contexto.Result = new ViewResult { ViewName = "SinConexion" };
            contexto.ExceptionHandled = true;
        }
    }
}
