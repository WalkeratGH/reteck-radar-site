using System.Text.Json;

namespace PartnerFinder.Services;

// Contact lookup backed by Hunter.io's Domain Search API.
// Given a company website, returns the best publicly listed email plus the
// person's name and job title when available. Free plan: ~25 searches/month.
//
// Get a free key at https://hunter.io (Dashboard -> API). Configure it in
// appsettings.json under "Contacts:Hunter:ApiKey". When set, contact fields
// are filled automatically (only when empty) during AI research.
public class HunterContactService : IContactFinder
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _baseUrl;

    public HunterContactService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(15);
        _apiKey = config["Contacts:Hunter:ApiKey"];
        _baseUrl = config["Contacts:Hunter:BaseUrl"] ?? "https://api.hunter.io";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ContactResult> FindAsync(string websiteOrDomain, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new ContactResult { Error = "Hunter.io is not configured" };

        var domain = ToDomain(websiteOrDomain);
        if (domain == null)
            return new ContactResult { Error = "Invalid website" };

        try
        {
            var url = $"{_baseUrl}/v2/domain-search?domain={Uri.EscapeDataString(domain)}&limit=10&api_key={Uri.EscapeDataString(_apiKey!)}";
            using var resp = await _http.GetAsync(url, ct);
            if ((int)resp.StatusCode == 401)
                return new ContactResult { Error = "Invalid Hunter.io API key (401)" };
            if ((int)resp.StatusCode == 429)
                return new ContactResult { Error = "Hunter.io quota exceeded (429)" };
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return Parse(doc.RootElement);
        }
        catch (Exception ex)
        {
            return new ContactResult { Error = $"Hunter.io lookup failed ({ex.GetType().Name})" };
        }
    }

    private static ContactResult Parse(JsonElement root)
    {
        var result = new ContactResult();
        if (!root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("emails", out var emails) ||
            emails.ValueKind != JsonValueKind.Array)
            return result;

        // Prefer the highest-confidence personal email (it carries a name/title);
        // fall back to the highest-confidence email of any type.
        JsonElement? best = null;
        JsonElement? bestPersonal = null;
        int bestScore = -1, bestPersonalScore = -1;

        foreach (var e in emails.EnumerateArray())
        {
            var score = e.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
            var type = GetStr(e, "type");
            if (score > bestScore) { best = e; bestScore = score; }
            if (type == "personal" && score > bestPersonalScore) { bestPersonal = e; bestPersonalScore = score; }
        }

        var pick = bestPersonal ?? best;
        if (pick == null) return result;

        var p = pick.Value;
        result.Email = GetStr(p, "value");
        var name = string.Join(" ", new[] { GetStr(p, "first_name"), GetStr(p, "last_name") }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        result.ContactPerson = string.IsNullOrWhiteSpace(name) ? null : name;
        result.ContactTitle = GetStr(p, "position");
        return result;
    }

    private static string? GetStr(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.String) return null;
        var s = v.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string? ToDomain(string websiteOrDomain)
    {
        var w = websiteOrDomain.Trim();
        if (!w.StartsWith("http", StringComparison.OrdinalIgnoreCase)) w = "https://" + w;
        return Uri.TryCreate(w, UriKind.Absolute, out var uri) ? uri.Host.Replace("www.", "") : null;
    }
}
