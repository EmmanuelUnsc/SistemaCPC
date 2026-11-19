using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Seguridad;

namespace SistemaCPC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/estado-cuenta")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class EstadoCuentaController : ControllerBase
{
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public EstadoCuentaController(EstadoDeCuenta estadoDeCuenta)
    {
        _estadoDeCuenta = estadoDeCuenta;
    }

    [HttpGet("{registro}")]
    [EndpointSummary("CU-02: estado de cuenta con los meses adeudados por año (el afiliado solo el suyo)")]
    [ProducesResponseType<EstadoCuentaRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCuentaRespuesta>> Obtener(string registro)
    {
        if (!User.PuedeVer(registro))
        {
            return StatusCode(403, new ErrorRespuesta("Solo puede ver su propia información."));
        }

        var estado = await _estadoDeCuenta.ObtenerEstado(registro);

        if (estado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        return estado;
    }

    [HttpGet("{registro}/tribunal")]
    [EndpointSummary("CU-03: saldo de tribunal: defensas por cobrar, deuda de aportes y saldo neto")]
    [ProducesResponseType<SaldoTribunalRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaldoTribunalRespuesta>> SaldoTribunal(string registro)
    {
        if (!User.PuedeVer(registro))
        {
            return StatusCode(403, new ErrorRespuesta("Solo puede ver su propia información."));
        }

        var saldo = await _estadoDeCuenta.ObtenerSaldoTribunal(registro);

        if (saldo == null)
        {
            return NotFound(new ErrorRespuesta("No existe un tribunal habilitado con ese registro."));
        }

        return saldo;
    }
}
