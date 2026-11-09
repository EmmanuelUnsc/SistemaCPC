using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Patrones.Singleton;

// Patrón Singleton: una sola instancia de los parámetros para todos los cálculos
// (CU-02, CU-03, CU-07 y CU-09). No se programa a mano: se registra como instancia única
// en Program.cs con builder.Services.AddSingleton<ParametrosSistema>().
public class ParametrosSistema
{
    public decimal AporteMensual { get; private set; }
    public decimal MontoPorDefensa { get; private set; }
    public int UmbralInactividad { get; private set; }
    public int TopeDefensasMes { get; private set; }

    // Se llama al iniciar la API, y cuando el directorio cambie un valor.
    public void Cargar(Parametros parametros)
    {
        AporteMensual = parametros.AporteMensual;
        MontoPorDefensa = parametros.MontoPorDefensa;
        UmbralInactividad = parametros.UmbralInactividad;
        TopeDefensasMes = parametros.TopeDefensasMes;
    }
}
