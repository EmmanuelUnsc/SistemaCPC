using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaCPC.Web.Models;

namespace SistemaCPC.Web.Servicios;

public record RespuestaApi<T>(T? Datos, string? Error)
{
    public bool Correcta => Error == null;
}

public class SesionVencidaException : Exception
{
}

// Patrón Proxy
public class ProxyApi
{
    private static readonly TimeSpan MargenDeRenovacion = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _clientes;
    private readonly IHttpContextAccessor _contexto;

    public ProxyApi(IHttpClientFactory clientes, IHttpContextAccessor contexto)
    {
        _clientes = clientes;
        _contexto = contexto;
    }

    public async Task<RespuestaApi<T>> Obtener<T>(string ruta)
    {
        var api = await Cliente();
        return await Leer<T>(await api.GetAsync(ruta));
    }

    public async Task<RespuestaApi<T>> Enviar<T>(HttpMethod metodo, string ruta, object cuerpo)
    {
        var api = await Cliente();
        return await Leer<T>(await api.SendAsync(new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(cuerpo) }));
    }

    public async Task<RespuestaApi<object>> EnviarArchivo(string ruta, string campo, IFormFile archivo)
    {
        var api = await Cliente();
        using var contenido = new MultipartFormDataContent();
        var datos = new StreamContent(archivo.OpenReadStream());
        datos.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType);
        contenido.Add(datos, campo, archivo.FileName);
        return await Leer<object>(await api.PutAsync(ruta, contenido));
    }

    public async Task<(byte[] Datos, string Tipo)?> ObtenerArchivo(string ruta)
    {
        var api = await Cliente();
        var respuesta = await api.GetAsync(ruta);

        if (!respuesta.IsSuccessStatusCode)
        {
            return null;
        }

        return (await respuesta.Content.ReadAsByteArrayAsync(),
            respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private async Task<HttpClient> Cliente()
    {
        var usuario = _contexto.HttpContext!.User;
        var token = usuario.FindFirst("Token")?.Value ?? throw new SesionVencidaException();
        var vence = DateTimeOffset.FromUnixTimeSeconds(long.Parse(usuario.FindFirst("TokenVence")!.Value));

        if (vence - DateTimeOffset.UtcNow < MargenDeRenovacion)
        {
            token = await Renovar(token, usuario);
        }

        var api = _clientes.CreateClient("Api");
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    private async Task<string> Renovar(string token, ClaimsPrincipal usuario)
    {
        var api = _clientes.CreateClient("Api");
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await api.PostAsync("api/auth/renovar", null);

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new SesionVencidaException();
        }

        var renovado = (await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>())!;
        var claims = usuario.Claims.Where(c => c.Type != "Token" && c.Type != "TokenVence").ToList();
        claims.AddRange(ClaimsDelToken(renovado));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var contexto = _contexto.HttpContext!;
        await contexto.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        contexto.User = principal;

        return renovado.Token;
    }

    public static IEnumerable<Claim> ClaimsDelToken(LoginRespuesta respuesta)
    {
        yield return new Claim("Token", respuesta.Token);
        yield return new Claim("TokenVence",
            new DateTimeOffset(DateTime.SpecifyKind(respuesta.Vence, DateTimeKind.Utc)).ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture));
    }

    private static async Task<RespuestaApi<T>> Leer<T>(HttpResponseMessage respuesta)
    {
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SesionVencidaException();
        }

        if (respuesta.StatusCode == HttpStatusCode.Forbidden)
        {
            return new RespuestaApi<T>(default, "No tiene permiso para esta acción.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await LeerMensaje(respuesta);
            return new RespuestaApi<T>(default, error ?? "La API no pudo completar la solicitud.");
        }

        if (respuesta.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object))
        {
            return new RespuestaApi<T>(default, null);
        }

        return new RespuestaApi<T>(await respuesta.Content.ReadFromJsonAsync<T>(), null);
    }

    private static async Task<string?> LeerMensaje(HttpResponseMessage respuesta)
    {
        try
        {
            var cuerpo = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

            if (cuerpo.TryGetProperty("mensaje", out var mensaje))
            {
                return mensaje.GetString();
            }

            if (cuerpo.TryGetProperty("errors", out var errores))
            {
                return string.Join(" ", errores.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString())));
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
