using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

[ApiController]
[Route("api/parametros")]
public class ParametrosController : ControllerBase
{
    private readonly ParametrosSistema _parametros;

    public ParametrosController(ParametrosSistema parametros)
    {
        _parametros = parametros;
    }

    [HttpGet]
    public ParametrosSistema Obtener() => _parametros;
}
