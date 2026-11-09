namespace SistemaCPC.Api.Models;

// Enumeraciones del diagrama de clases (Documento de Diseño).

public enum EstadoAfiliado
{
    Activo,
    Inactivo,
    Renunciado,
    Fallecido
}

public enum EstadoDefensa
{
    PorPagar,
    Pagada
}

public enum OrigenPago
{
    Plataforma,
    Oficina
}
