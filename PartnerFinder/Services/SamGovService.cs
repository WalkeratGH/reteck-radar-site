using System.Text.Json;

namespace PartnerFinder.Services;

// One company found in the SAM.gov federal-contractor registry.
public record SamEntity(string Name, string? Website, string? City, string? State, string? Uei);

public class SamGovResult
{
    public List<SamEntity> Entities { get; } = new();
    public int TotalRecords { get; set; }
    public string? Error { get; set; }
}

// Discovery source backed by the official (free) SAM.gov Entity Management API.
// SAM.gov lists every company registered to do business with the US federal
// government - searchable by state and industry code (NAICS), which makes it a
// clean, legal source of US IT-service-provider names and websites.
//
// Get a free key: register at https://sam.gov -> Account Details -> API Key.
// Configure it in appsettings.json under "Discovery:SamGov:ApiKey".
public class SamGovService
{
    // Common NAICS codes for the provider types Re-Teck looks for.
    public static readonly (string Code, string Label)[] NaicsOptions =
    {
        ("541512", "541512 - Computer Systems Design (SI / integrators)"),
        ("541513", "541513 - Computer Facilities Management"),
        ("541519", "541519 - Other Computer Related Services"),
        ("811212", "811212 - Computer & Office Machine Repair"),
        ("423430", "423430 - Computer Equipment Merchant Wholesalers"),
        ("518210", "518210 - Data Processing, Hosting & Related"),
    };

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _baseUrl;

    public SamGovService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(30);
        _apiKey = config["Discovery:SamGov:ApiKey"];
        _baseUrl = config["Discovery:SamGov:BaseUrl"] ?? "https://api.sam.gov";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<SamGovResult> SearchAsync(string? state, string? naics, string? name, CancellationToken ct = default)
    {
        var result = new SamGovResult();
        if (!IsConfigured)
        {
            result.Error = "SAM.gov is not configured";
            return result;
        }

        // Note: the SAM.gov Entity API caps "size" at 10 records per page
        // (a larger value returns HTTP 400 "size is N").
        var url = $"{_baseUrl}/entity-information/v3/entities?api_key={Uri.EscapeDataString(_apiKey!)}" +
                  "&registrationStatus=A&includeSections=entityRegistration,coreData&size=10";
        if (!string.IsNullOrWhiteSpace(state)) url += $"&physicalAddressProvinceOrStateCode={Uri.EscapeDataString(state.Trim().ToUpperInvariant())}";
        if (!string.IsNullOrWhiteSpace(naics)) url += $"&primaryNaics={Uri.EscapeDataString(naics.Trim())}";
        if (!string.IsNullOrWhiteSpace(name)) url += $"&legalBusinessName={Uri.EscapeDataString(name.Trim())}";

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            var status = (int)resp.StatusCode;

            if (status is 401 or 403)
            {
                result.Error = "SAM.gov rejected the API key (401/403). In your SAM.gov profile go to " +
                               "Account Details and make sure the Public API Key is generated and copied " +
                               "correctly. A brand-new key can take a few minutes to activate.";
                return result;
            }
            if (status == 429)
            {
                result.Error = "SAM.gov rate limit reached (429) - the daily quota is used up. Try again later.";
                return result;
            }

            var payload = await resp.Content.ReadAsStringAsync(ct);

            if (status == 404)
            {
                result.Error = "SAM.gov returned HTTP 404 (endpoint not found). The API path may have changed - " +
                               "please send me this message so I can update it. " + ShortBody(payload);
                return result;
            }
            if (status < 200 || status >= 300)
            {
                // Surface SAM.gov's own explanation (it usually returns a JSON
                // error body describing the bad parameter) instead of a generic
                // exception name, so the real cause is visible.
                result.Error = $"SAM.gov returned HTTP {status}. {ExtractApiMessage(payload)}";
                return result;
            }

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("totalRecords", out var total) && total.ValueKind == JsonValueKind.Number)
                result.TotalRecords = total.GetInt32();

            if (root.TryGetProperty("entityData", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in list.EnumerateArray())
                {
                    var reg = e.TryGetProperty("entityRegistration", out var r) ? r : default;
                    var core = e.TryGetProperty("coreData", out var cd) ? cd : default;

                    var entityName = GetStr(reg, "legalBusinessName");
                    if (string.IsNullOrWhiteSpace(entityName)) continue;

                    string? website = null, city = null, st = null;
                    if (core.ValueKind == JsonValueKind.Object)
                    {
                        if (core.TryGetProperty("entityInformation", out var info))
                            website = GetStr(info, "entityURL");
                        if (core.TryGetProperty("physicalAddress", out var addr))
                        {
                            city = GetStr(addr, "city");
                            st = GetStr(addr, "stateOrProvinceCode");
                        }
                    }

                    result.Entities.Add(new SamEntity(entityName!, website, city, st, GetStr(reg, "ueiSAM")));
                }
            }
        }
        catch (TaskCanceledException)
        {
            result.Error = "SAM.gov timed out (no response within 30s). Check your internet connection and try again.";
        }
        catch (HttpRequestException ex)
        {
            result.Error = $"Could not reach SAM.gov ({ex.Message}). Check your internet connection or a firewall/VPN.";
        }
        catch (JsonException)
        {
            result.Error = "SAM.gov returned a response that could not be read (unexpected format).";
        }
        catch (Exception ex)
        {
            result.Error = $"SAM.gov lookup failed: {ex.Message}";
        }
        return result;
    }

    private static string? GetStr(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.String) return null;
        var s = v.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    // Pulls a human-readable message out of SAM.gov's JSON error body when
    // possible (it varies: "error", "message", "errorMessage", or a nested
    // description), otherwise falls back to a trimmed snippet of the raw body.
    private static string ExtractApiMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "(no details returned)";
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            foreach (var key in new[] { "error_description", "errorMessage", "message", "error" })
            {
                if (root.TryGetProperty(key, out var v))
                {
                    if (v.ValueKind == JsonValueKind.String) return v.GetString()!;
                    if (v.ValueKind == JsonValueKind.Object && v.TryGetProperty("message", out var m)
                        && m.ValueKind == JsonValueKind.String) return m.GetString()!;
                }
            }
        }
        catch (JsonException) { /* not JSON - fall through to snippet */ }
        return ShortBody(body);
    }

    private static string ShortBody(string body)
    {
        body = body.Trim();
        if (body.Length == 0) return "(empty response)";
        return body.Length > 300 ? body[..300] + "…" : body;
    }
}
