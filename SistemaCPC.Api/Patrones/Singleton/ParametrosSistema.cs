using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Patrones.Singleton;

// Patrón Singleton
public class ParametrosSistema
{
    public decimal AporteMensual { get; private set; }
    public decimal MontoPorDefensa { get; private set; }
    public int UmbralInactividad { get; private set; }
    public int TopeDefensasMes { get; private set; }

    public void Cargar(Parametros parametros)
    {
        AporteMensual = parametros.AporteMensual;
        MontoPorDefensa = parametros.MontoPorDefensa;
        UmbralInactividad = parametros.UmbralInactividad;
        TopeDefensasMes = parametros.TopeDefensasMes;
    }
}
