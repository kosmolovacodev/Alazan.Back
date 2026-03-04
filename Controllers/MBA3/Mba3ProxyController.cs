using Microsoft.AspNetCore.Mvc;

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
        if (Request.Headers.TryGetValue("Authorization", out var auth))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", (string)auth);

        // Leer body completo en memoria para que HttpClient establezca Content-Length
        // (StreamContent no conoce el tamaño → envía chunked → MBA3 no lo acepta)
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

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = contentType,
        };
    }
}
