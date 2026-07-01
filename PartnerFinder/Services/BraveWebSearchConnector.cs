using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PartnerFinder.Services;

// Live web-search connector backed by the Brave Search API.
// Reads the API key from configuration ("Search:Brave:ApiKey" in appsettings.json,
// or the environment variable Search__Brave__ApiKey). When no key is set,
// IsConfigured is false and the rest of the app treats search as unavailable.
//
// Free key: https://brave.com/search/api/  (Free plan includes a monthly quota.)
public class BraveWebSearchConnector : IWebSearchConnector
{
    private const string Endpoint = "https://api.search.brave.com/res/v1/web/search";

    private readonly HttpClient _http;
    private readonly string? _apiKey;

    public BraveWebSearchConnector(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Search:Brave:ApiKey"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchHit>();

        var url = $"{Endpoint}?q={Uri.EscapeDataString(query)}&count=15";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("X-Subscription-Token", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var hits = new List<SearchHit>();
        if (doc.RootElement.TryGetProperty("web", out var web) &&
            web.TryGetProperty("results", out var results) &&
            results.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in results.EnumerateArray())
            {
                var link = GetString(r, "url");
                if (string.IsNullOrWhiteSpace(link)) continue;
                hits.Add(new SearchHit(
                    StripHtml(GetString(r, "title")),
                    link,
                    StripHtml(GetString(r, "description"))));
            }
        }
        return hits;
    }

    private static string GetString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? string.Empty
            : string.Empty;

    // Brave highlights matches with <strong> tags; strip any HTML for clean display.
    private static string StripHtml(string s) => Regex.Replace(s, "<.*?>", string.Empty);
}
