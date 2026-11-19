using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Seguridad;

namespace SistemaCPC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/estado-cuenta")]
public class EstadoCuentaController : ControllerBase
{
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public EstadoCuentaController(EstadoDeCuenta estadoDeCuenta)
    {
        _estadoDeCuenta = estadoDeCuenta;
    }

    [HttpGet("{registro}")]
    public async Task<ActionResult<EstadoCuentaRespuesta>> Obtener(string registro)
    {
        if (!User.PuedeVer(registro))
        {
            return StatusCode(403, new { mensaje = "Solo puede ver su propia información." });
        }

        var estado = await _estadoDeCuenta.ObtenerEstado(registro);

        if (estado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        return estado;
    }

    [HttpGet("{registro}/tribunal")]
    public async Task<ActionResult<SaldoTribunalRespuesta>> SaldoTribunal(string registro)
    {
        if (!User.PuedeVer(registro))
        {
            return StatusCode(403, new { mensaje = "Solo puede ver su propia información." });
        }

        var saldo = await _estadoDeCuenta.ObtenerSaldoTribunal(registro);

        if (saldo == null)
        {
            return NotFound(new { mensaje = "No existe un tribunal habilitado con ese registro." });
        }

        return saldo;
    }
}
