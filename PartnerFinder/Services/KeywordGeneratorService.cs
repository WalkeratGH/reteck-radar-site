namespace PartnerFinder.Services;

// Input from the "Search Keyword Generator" page.
public class KeywordRequest
{
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? ServiceType { get; set; }
    public bool AiCapabilityRequired { get; set; }
}

// Generates ready-to-paste Google/Bing search phrases from a location + service type.
// No paid API is used - the user copies the phrases, searches manually, then files
// candidate companies via Add Partner. (A real Web Search connector can be plugged
// in later; see Services/Connectors.)
public class KeywordGeneratorService
{
    // Base IT / SI search angles.
    private static readonly string[] ItTemplates =
    {
        "IT system integrator {place}",
        "data center smart hands provider {place}",
        "IMAC service provider {place}",
        "break fix IT support company {place}",
        "network server storage support {place}",
        "IT field service provider {place}",
        "Microsoft partner IT services {place}",
        "Dell partner server support {place}",
        "Cisco partner network integrator {place}",
        "HPE partner data center services {place}"
    };

    // Additional AI-infrastructure search angles (only when AI capability is required).
    private static readonly string[] AiTemplates =
    {
        "AI server builder {place}",
        "GPU workstation provider {place}",
        "edge AI deployment partner {place}",
        "NVIDIA Jetson solution provider {place}",
        "local LLM deployment service provider {place}",
        "on-prem AI deployment company {place}",
        "data center smart hands GPU server support {place}",
        "small AI cluster integrator {place}",
        "AI infrastructure system integrator Dell NVIDIA partner {place}"
    };

    public List<string> Generate(KeywordRequest req)
    {
        // Build the "place" fragment: prefer "City, Country", fall back to whichever exists.
        var parts = new[] { req.City, req.Country }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim());
        var place = string.Join(", ", parts);

        var results = new List<string>();

        // If a specific service type was chosen, lead with focused phrases for it.
        if (!string.IsNullOrWhiteSpace(req.ServiceType))
        {
            var svc = req.ServiceType.Trim();
            results.Add(Compose($"{svc}", place));
            results.Add(Compose($"{svc} company", place));
            if (req.AiCapabilityRequired)
                results.Add(Compose($"{svc} AI server GPU deployment", place));
        }

        foreach (var t in ItTemplates)
            results.Add(Compose(t, place));

        if (req.AiCapabilityRequired)
            foreach (var t in AiTemplates)
                results.Add(Compose(t, place));

        // De-duplicate while preserving order, drop empties.
        return results
            .Select(Normalize)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Compose(string template, string place)
    {
        if (template.Contains("{place}"))
            return template.Replace("{place}", place);
        return string.IsNullOrWhiteSpace(place) ? template : $"{template} {place}";
    }

    private static string Normalize(string s)
        => string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim().TrimEnd(',');
}
