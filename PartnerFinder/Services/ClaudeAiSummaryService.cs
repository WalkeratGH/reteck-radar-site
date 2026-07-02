using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using PartnerFinder.Models;

namespace PartnerFinder.Services;

// Live AI Summary generator backed by the Claude API (official Anthropic SDK).
// Reads the API key from configuration ("Ai:Anthropic:ApiKey" in appsettings.json).
// When no key is set, IsConfigured is false and the UI shows setup instructions.
//
// Get a key at https://console.anthropic.com (API Keys -> Create Key).
// The model is configurable via "Ai:Anthropic:Model" (default: claude-opus-4-8;
// set "claude-haiku-4-5" for the cheapest option).
public class ClaudeAiSummaryService : IAiSummaryGenerator
{
    // Capability names Claude may suggest - shown to the human for verification,
    // never auto-applied. Keep in sync with the Partner capability fields.
    private static readonly string[] CapabilityNames =
    {
        "Data Center Experience", "Smart Hands", "IMAC", "Break/Fix",
        "Network Support", "Server Support", "Storage Support",
        "AI Server Build", "GPU Workstation Build", "Edge AI Deployment",
        "Local LLM Deployment", "NVIDIA GPU Experience", "AMD GPU Experience",
        "NVIDIA Jetson / Edge Device", "Small AI Cluster", "On-Prem AI Deployment",
        "AI Model Inference Setup", "Linux/Docker/Kubernetes", "Cooling/Power Planning"
    };

    private readonly string? _apiKey;
    private readonly string _model;
    private readonly WebsiteInfoService _webInfo;

    public ClaudeAiSummaryService(IConfiguration config, WebsiteInfoService webInfo)
    {
        _apiKey = config["Ai:Anthropic:ApiKey"];
        var model = config["Ai:Anthropic:Model"];
        _model = string.IsNullOrWhiteSpace(model) ? "claude-opus-4-8" : model!;
        _webInfo = webInfo;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiSummaryResult> SummarizeAsync(Partner partner, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new AiSummaryResult { Error = "No Anthropic API key configured (Ai:Anthropic:ApiKey)" };

        // Best effort: pull the company website's own description for extra context.
        string? websiteDescription = null;
        if (!string.IsNullOrWhiteSpace(partner.Website))
        {
            try
            {
                var info = await _webInfo.FetchAsync(partner.Website!, ct);
                websiteDescription = info.Description;
            }
            catch
            {
                // Website context is a bonus; ignore failures.
            }
        }

        try
        {
            AnthropicClient client = new() { ApiKey = _apiKey };

            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                MaxTokens = 8000,
                Thinking = new ThinkingConfigAdaptive(),
                System = SystemPrompt,
                OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = BuildSchema() } },
                Messages = [new() { Role = Role.User, Content = BuildProfile(partner, websiteDescription) }],
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
        "You are a partner-qualification analyst at Re-Teck, an ITAD / data-center asset " +
        "recovery and hardware reuse company. Re-Teck looks for IT / AI infrastructure " +
        "system integrators and service providers that consume GPUs, RAM and servers in " +
        "their equipment-leasing and SME services - they matter as buyers of refurbished " +
        "hardware, as channel partners, and as recycling sources. " +
        "Write concise, factual business English. Base every statement strictly on the " +
        "provided data; when information is missing, say what should be verified instead " +
        "of guessing.";

    private static string BuildProfile(Partner p, string? websiteDescription)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Evaluate this candidate partner for Re-Teck and fill the JSON schema.");
        sb.AppendLine("- summary: 3-5 sentences on fit with Re-Teck's goals (hardware demand, leasing/SME signals, partnership value).");
        sb.AppendLine("- ai_infrastructure_summary: 1-3 sentences on their AI infrastructure capability.");
        sb.AppendLine("- suggested_capabilities: capabilities the data suggests but that are NOT yet checked below (empty array if none).");
        sb.AppendLine("- suggested_follow_up: one concrete next action for our team.");
        sb.AppendLine();
        sb.AppendLine("=== Company profile ===");
        sb.AppendLine($"Company: {p.CompanyName}");
        Add(sb, "Location", string.Join(", ", new[] { p.City, p.Country }.Where(s => !string.IsNullOrWhiteSpace(s))));
        Add(sb, "Website", p.Website);
        Add(sb, "Service category", p.ServiceCategory);
        Add(sb, "Main services", p.MainServices);
        Add(sb, "Website description", websiteDescription);
        Add(sb, "Certifications", p.Certifications);
        Add(sb, "Brand partnerships",
            string.Join(", ", new[]
            {
                p.MicrosoftPartnerStatus != PartnerStatus.None ? $"Microsoft ({p.MicrosoftPartnerStatus})" : null,
                p.DellPartnerStatus != PartnerStatus.None ? $"Dell ({p.DellPartnerStatus})" : null,
                p.CiscoPartnerStatus != PartnerStatus.None ? $"Cisco ({p.CiscoPartnerStatus})" : null,
                p.HpePartnerStatus != PartnerStatus.None ? $"HPE ({p.HpePartnerStatus})" : null,
            }.Where(s => s != null)));
        Add(sb, "Notes", p.Notes);
        Add(sb, "Existing AI infrastructure summary", p.AiInfrastructureSummary);

        sb.AppendLine();
        sb.AppendLine("=== Capabilities already checked ===");
        var check = new (string Name, bool On)[]
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
        var onList = check.Where(c => c.On).Select(c => c.Name).ToList();
        sb.AppendLine(onList.Count > 0 ? string.Join(", ", onList) : "(none)");

        return sb.ToString();
    }

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
            suggested_capabilities = new
            {
                type = "array",
                items = new { type = "string", @enum = CapabilityNames },
            },
            suggested_follow_up = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[]
        {
            "summary", "ai_infrastructure_summary", "suggested_capabilities", "suggested_follow_up",
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
            SuggestedFollowUp = GetString(root, "suggested_follow_up"),
        };
        if (root.TryGetProperty("suggested_capabilities", out var caps) && caps.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in caps.EnumerateArray())
            {
                if (c.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(c.GetString()))
                    result.SuggestedCapabilities.Add(c.GetString()!);
            }
        }
        return result;
    }

    private static string? GetString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
