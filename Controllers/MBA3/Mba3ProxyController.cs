using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

[ApiController]
[Route("v1/mba3")]
public class Mba3ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string MBA3_BASE = "http://201.148.25.52:8443";

    public Mba3ProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{**path}")]
    [HttpPost("{**path}")]
    [HttpPut("{**path}")]
    [HttpDelete("{**path}")]
    public async Task<IActionResult> Proxy(string path)
    {
        // Permitir releer el body si algún middleware lo consumió antes
        Request.EnableBuffering();

        var client = _httpClientFactory.CreateClient();

        // Usamos Request.Path directamente para preservar la barra final
        // (el parámetro {**path} puede normalizarla y quitarla)
        const string routePrefix = "/v1/mba3";
        var mba3Path = Request.Path.Value!.Substring(routePrefix.Length);
        var targetUrl = MBA3_BASE + mba3Path;
        if (Request.QueryString.HasValue)
            targetUrl += Request.QueryString.Value;

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(Request.Method),
            RequestUri = new Uri(targetUrl),
        };

        // Reenviar cabecera Authorization (token MBA3)
        // IIS puede eliminar el header Authorization estándar → usamos X-MBA3-Auth como fallback
        string? authValue = null;
        bool hasAuthHeader = Request.Headers.ContainsKey("Authorization");
        bool hasMba3Header = Request.Headers.ContainsKey("X-MBA3-Auth");

        if (Request.Headers.TryGetValue("Authorization", out var auth) && !string.IsNullOrWhiteSpace(auth.ToString()))
            authValue = auth.ToString();
        else if (Request.Headers.TryGetValue("X-MBA3-Auth", out var bypassAuth) && !string.IsNullOrWhiteSpace(bypassAuth.ToString()))
            authValue = bypassAuth.ToString();

        if (authValue != null)
        {
            // TryAddWithoutValidation puede fallar para JWTs crudos (contienen puntos no válidos como scheme)
            // Si falla, usamos la propiedad Authorization tipada con el JWT como scheme
            if (!requestMessage.Headers.TryAddWithoutValidation("Authorization", authValue))
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(authValue);
        }

        // Leer body completo en memoria para que HttpClient establezca Content-Length
        // (StreamContent no conoce el tamaño → envía chunked → MBA3 no lo acepta)
        Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        if (ms.Length > 0)
        {
            requestMessage.Content = new ByteArrayContent(ms.ToArray());
            if (Request.Headers.TryGetValue("Content-Type", out var ct))
                requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", (string)ct);
        }

        var response = await client.SendAsync(requestMessage);
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8";

        // DEBUG TEMPORAL: headers para diagnóstico (remover cuando funcione)
        Response.Headers.Append("X-Debug-Has-Auth", hasAuthHeader.ToString());
        Response.Headers.Append("X-Debug-Has-MBA3", hasMba3Header.ToString());
        Response.Headers.Append("X-Debug-Auth-Set", (authValue != null).ToString());
        Response.Headers.Append("X-Debug-Body-Len", ms.Length.ToString());

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = contentType,
        };
    }
}
