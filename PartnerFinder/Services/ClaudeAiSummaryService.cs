using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using PartnerFinder.Models;

namespace PartnerFinder.Services;

// AI deep research backed by the Claude API (official Anthropic SDK).
//
// Gathers evidence first - the company's website text (homepage + a few key
// pages) and, when Brave search is configured, recent web-search snippets -
// then asks Claude to fill the partner's fields from that evidence:
// capabilities, brand partnerships, contacts, leasing / SME targeting signals,
// and an evaluation summary. Only evidence-backed values are returned.
//
// Reads the API key from configuration ("Ai:Anthropic:ApiKey" in appsettings.json).
// Get a key at https://console.anthropic.com (API Keys -> Create Key).
// Model is configurable via "Ai:Anthropic:Model" (default: claude-opus-4-8;
// set "claude-haiku-4-5" for the cheapest option).
public class ClaudeAiSummaryService : IAiSummaryGenerator
{
    // Capability names Claude may report. Keep in sync with the Partner fields
    // and with PartnersController.ApplyResearch's mapping.
    private static readonly string[] CapabilityNames =
    {
        "Data Center Experience", "Smart Hands", "IMAC", "Break/Fix",
        "Network Support", "Server Support", "Storage Support",
        "AI Server Build", "GPU Workstation Build", "Edge AI Deployment",
        "Local LLM Deployment", "NVIDIA GPU Experience", "AMD GPU Experience",
        "NVIDIA Jetson / Edge Device", "Small AI Cluster", "On-Prem AI Deployment",
        "AI Model Inference Setup", "Linux/Docker/Kubernetes", "Cooling/Power Planning"
    };

    private static readonly string[] BrandNames = { "Microsoft", "Dell", "Cisco", "HPE" };

    private readonly string? _apiKey;
    private readonly string _model;
    private readonly WebsiteInfoService _webInfo;
    private readonly IWebSearchConnector _search;

    public ClaudeAiSummaryService(IConfiguration config, WebsiteInfoService webInfo, IWebSearchConnector search)
    {
        _apiKey = config["Ai:Anthropic:ApiKey"];
        var model = config["Ai:Anthropic:Model"];
        _model = string.IsNullOrWhiteSpace(model) ? "claude-opus-4-8" : model!;
        _webInfo = webInfo;
        _search = search;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiSummaryResult> SummarizeAsync(Partner partner, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new AiSummaryResult { Error = "No Anthropic API key configured (Ai:Anthropic:ApiKey)" };

        // --- Evidence gathering (best effort, failures ignored) ---
        string siteText = "";
        if (!string.IsNullOrWhiteSpace(partner.Website))
        {
            try { siteText = await _webInfo.FetchSiteTextAsync(partner.Website!, ct); }
            catch { /* website text is a bonus */ }
        }

        string searchText = "";
        if (_search.IsConfigured)
        {
            try
            {
                var query = $"\"{partner.CompanyName}\" {partner.City} {partner.Country} IT services AI GPU leasing";
                var hits = await _search.SearchAsync(query, 0, ct);
                searchText = string.Join("\n", hits.Take(5).Select(h => $"- {h.Title} | {h.Url} | {h.Snippet}"));
            }
            catch { /* search snippets are a bonus */ }
        }

        try
        {
            AnthropicClient client = new() { ApiKey = _apiKey };

            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                MaxTokens = 12000,
                Thinking = new ThinkingConfigAdaptive(),
                System = SystemPrompt,
                OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = BuildSchema() } },
                Messages = [new() { Role = Role.User, Content = BuildPrompt(partner, siteText, searchText) }],
            });

            var json = response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .Select(t => t.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(json))
                return new AiSummaryResult { Error = "The model returned no text (possibly refused)" };

            return Parse(json!);
        }
        catch (Anthropic.Exceptions.AnthropicUnauthorizedException)
        {
            return new AiSummaryResult { Error = "Invalid Anthropic API key (401) - check appsettings.json" };
        }
        catch (Anthropic.Exceptions.AnthropicRateLimitException)
        {
            return new AiSummaryResult { Error = "Rate limited by the Claude API (429) - wait a minute and retry" };
        }
        catch (Anthropic.Exceptions.AnthropicApiException ex)
        {
            return new AiSummaryResult { Error = $"Claude API error: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new AiSummaryResult { Error = $"Could not reach the Claude API ({ex.GetType().Name})" };
        }
    }

    private const string SystemPrompt =
        "You are a partner-qualification researcher at Re-Teck, an ITAD / data-center asset " +
        "recovery and hardware reuse company. Re-Teck looks for IT / AI infrastructure system " +
        "integrators and service providers that consume GPUs, RAM and servers in their " +
        "equipment-leasing and SME services - they matter as buyers of refurbished hardware, " +
        "as channel partners, and as recycling sources. " +
        "Fill the JSON strictly from the evidence provided (company record, website text, web " +
        "search snippets). Only claim a capability, partnership or signal when the evidence " +
        "supports it; when unknown, return an empty string / empty array / false. " +
        "Never invent contact details - only return an email or phone that literally appears " +
        "in the evidence. Write concise, factual business English.";

    private static string BuildPrompt(Partner p, string siteText, string searchText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Research this candidate partner for Re-Teck and fill the JSON schema.");
        sb.AppendLine("Field notes:");
        sb.AppendLine("- summary: 3-5 sentences on fit with Re-Teck (hardware demand, leasing/SME signals, partnership value) plus what to verify.");
        sb.AppendLine("- capabilities: EVERY capability the evidence supports (including ones already checked).");
        sb.AppendLine("- brand_partnerships: brands where a partner/reseller relationship is evident.");
        sb.AppendLine("- equipment_leasing: true if they lease/rent hardware or sell as-a-service.");
        sb.AppendLine("- sme_focus: true if their customers are mainly small/medium businesses.");
        sb.AppendLine("- city/country/email/phone/main_services/certifications: fill from evidence, empty string if not evident.");
        sb.AppendLine("- suggested_follow_up: one concrete next action for our team.");
        sb.AppendLine();
        sb.AppendLine("=== Current record ===");
        sb.AppendLine($"Company: {p.CompanyName}");
        Add(sb, "Location", string.Join(", ", new[] { p.City, p.Country }.Where(s => !string.IsNullOrWhiteSpace(s))));
        Add(sb, "Website", p.Website);
        Add(sb, "Service category", p.ServiceCategory);
        Add(sb, "Main services", p.MainServices);
        Add(sb, "Certifications", p.Certifications);
        Add(sb, "Notes", p.Notes);

        var check = CurrentCapabilities(p).Where(c => c.On).Select(c => c.Name).ToList();
        sb.AppendLine($"Capabilities already checked: {(check.Count > 0 ? string.Join(", ", check) : "(none)")}");

        if (!string.IsNullOrWhiteSpace(siteText))
        {
            sb.AppendLine();
            sb.AppendLine("=== Company website text ===");
            sb.AppendLine(siteText);
        }
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            sb.AppendLine();
            sb.AppendLine("=== Web search snippets ===");
            sb.AppendLine(searchText);
        }
        return sb.ToString();
    }

    internal static (string Name, bool On)[] CurrentCapabilities(Partner p) => new[]
    {
        ("Data Center Experience", p.DataCenterExperience), ("Smart Hands", p.SmartHandsCapability),
        ("IMAC", p.ImacCapability), ("Break/Fix", p.BreakFixCapability),
        ("Network Support", p.NetworkSupportCapability), ("Server Support", p.ServerSupportCapability),
        ("Storage Support", p.StorageSupportCapability), ("AI Server Build", p.AiServerBuildCapability),
        ("GPU Workstation Build", p.GpuWorkstationBuildCapability), ("Edge AI Deployment", p.EdgeAiDeploymentCapability),
        ("Local LLM Deployment", p.LocalLlmDeploymentCapability), ("NVIDIA GPU Experience", p.NvidiaGpuExperience),
        ("AMD GPU Experience", p.AmdGpuExperience), ("NVIDIA Jetson / Edge Device", p.NvidiaJetsonExperience),
        ("Small AI Cluster", p.SmallAiClusterExperience), ("On-Prem AI Deployment", p.OnPremAiDeploymentExperience),
        ("AI Model Inference Setup", p.AiModelInferenceSetup), ("Linux/Docker/Kubernetes", p.LinuxDockerKubernetesCapability),
        ("Cooling/Power Planning", p.CoolingPowerPlanningCapability),
    };

    private static void Add(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{label}: {value}");
    }

    private static Dictionary<string, JsonElement> BuildSchema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            summary = new { type = "string" },
            ai_infrastructure_summary = new { type = "string" },
            main_services = new { type = "string" },
            certifications = new { type = "string" },
            city = new { type = "string" },
            country = new { type = "string" },
            email = new { type = "string" },
            phone = new { type = "string" },
            capabilities = new { type = "array", items = new { type = "string", @enum = CapabilityNames } },
            brand_partnerships = new { type = "array", items = new { type = "string", @enum = BrandNames } },
            equipment_leasing = new { type = "boolean" },
            sme_focus = new { type = "boolean" },
            suggested_follow_up = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[]
        {
            "summary", "ai_infrastructure_summary", "main_services", "certifications",
            "city", "country", "email", "phone", "capabilities", "brand_partnerships",
            "equipment_leasing", "sme_focus", "suggested_follow_up",
        }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private static AiSummaryResult Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = new AiSummaryResult
        {
            Summary = GetString(root, "summary"),
            AiInfrastructureSummary = GetString(root, "ai_infrastructure_summary"),
            MainServices = GetString(root, "main_services"),
            Certifications = GetString(root, "certifications"),
            City = GetString(root, "city"),
            Country = GetString(root, "country"),
            Email = GetString(root, "email"),
            Phone = GetString(root, "phone"),
            SuggestedFollowUp = GetString(root, "suggested_follow_up"),
            EquipmentLeasing = GetBool(root, "equipment_leasing"),
            SmeFocus = GetBool(root, "sme_focus"),
        };
        FillList(root, "capabilities", result.SuggestedCapabilities);
        FillList(root, "brand_partnerships", result.BrandPartnerships);
        return result;
    }

    private static void FillList(JsonElement root, string name, List<string> target)
    {
        if (root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    target.Add(item.GetString()!);
    }

    private static string? GetString(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.String) return null;
        var s = v.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static bool? GetBool(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False)
            ? v.GetBoolean()
            : null;
}
