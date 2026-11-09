using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

[ApiController]
[Route("api/parametros")]
public class ParametrosController : ControllerBase
{
    private readonly ParametrosSistema _parametros;

    // ASP.NET Core entrega siempre la misma instancia (Singleton).
    public ParametrosController(ParametrosSistema parametros)
    {
        _parametros = parametros;
    }

    // GET api/parametros
    [HttpGet]
    public ParametrosSistema Obtener() => _parametros;
}
