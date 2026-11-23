using System.Net;
using SistemaCPC.Web.Models;

namespace SistemaCPC.Web.Servicios;

public record ResultadoLogin(LoginRespuesta? Usuario, string? Error);

public interface IServicioAutenticacion
{
    Task<ResultadoLogin> IniciarSesion(string registro, string contrasena);
}

// Patrón Proxy
public class ProxyAutenticacion : IServicioAutenticacion
{
    private readonly IHttpClientFactory _clientes;

    public ProxyAutenticacion(IHttpClientFactory clientes)
    {
        _clientes = clientes;
    }

    public async Task<ResultadoLogin> IniciarSesion(string registro, string contrasena)
    {
        var api = _clientes.CreateClient("Api");

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await api.PostAsJsonAsync("api/auth/login", new { registro, contrasena });
        }
        catch (HttpRequestException)
        {
            return new ResultadoLogin(null, "No se pudo conectar con la API. Verifique que SistemaCPC.Api esté en ejecución.");
        }

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new ResultadoLogin(null, "Registro o contraseña incorrectos.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return new ResultadoLogin(null, "Ingrese su registro y su contraseña.");
        }

        return new ResultadoLogin(await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>(), null);
    }
}
