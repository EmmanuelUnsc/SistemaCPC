using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Servicios;

public class ServicioBitacora
{
    private readonly CpcContext _context;

    public ServicioBitacora(CpcContext context)
    {
        _context = context;
    }

    public void Anotar(string usuario, string motivo, decimal valorAnterior, decimal valorNuevo)
    {
        _context.Bitacora.Add(new RegistroBitacora
        {
            Fecha = DateTime.UtcNow,
            Usuario = usuario,
            Motivo = motivo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo
        });
    }
}
