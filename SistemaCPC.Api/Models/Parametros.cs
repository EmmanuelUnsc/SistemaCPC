namespace SistemaCPC.Api.Models;

// Valores que se usan en los cálculos y se pueden cambiar sin tocar el código (RNF-07).
// Se guardan en la base de datos (una sola fila).
public class Parametros
{
    public int Id { get; set; }

    public decimal AporteMensual { get; set; }

    public decimal MontoPorDefensa { get; set; }

    public int UmbralInactividad { get; set; }

    public int TopeDefensasMes { get; set; }
}
