using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Patrones.Facade;

namespace SistemaCPC.Api.Controllers;

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
        var saldo = await _estadoDeCuenta.ObtenerSaldoTribunal(registro);

        if (saldo == null)
        {
            return NotFound(new { mensaje = "No existe un tribunal habilitado con ese registro." });
        }

        return saldo;
    }
}
