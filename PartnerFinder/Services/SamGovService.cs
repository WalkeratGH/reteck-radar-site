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

        var url = $"{_baseUrl}/entity-information/v3/entities?api_key={Uri.EscapeDataString(_apiKey!)}" +
                  "&registrationStatus=A&includeSections=entityRegistration,coreData&size=30";
        if (!string.IsNullOrWhiteSpace(state)) url += $"&physicalAddressProvinceOrStateCode={Uri.EscapeDataString(state.Trim().ToUpperInvariant())}";
        if (!string.IsNullOrWhiteSpace(naics)) url += $"&primaryNaics={Uri.EscapeDataString(naics.Trim())}";
        if (!string.IsNullOrWhiteSpace(name)) url += $"&legalBusinessName={Uri.EscapeDataString(name.Trim())}";

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if ((int)resp.StatusCode is 401 or 403)
            {
                result.Error = "SAM.gov rejected the API key (check it in your SAM.gov profile)";
                return result;
            }
            if ((int)resp.StatusCode == 429)
            {
                result.Error = "SAM.gov rate limit reached - try again later";
                return result;
            }
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
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
        catch (Exception ex)
        {
            result.Error = $"SAM.gov lookup failed ({ex.GetType().Name})";
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
}
