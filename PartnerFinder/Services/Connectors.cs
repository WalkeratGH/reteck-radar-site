using PartnerFinder.Models;

namespace PartnerFinder.Services;

// ---------------------------------------------------------------------------
// FUTURE EXPANSION - placeholders only.
//
// These interfaces reserve the architecture for later phases. Nothing here calls
// a paid API today. When you are ready, add a real implementation and register it
// in Program.cs. The rest of the app already depends on the interfaces, so wiring
// in a live connector will not require rewrites.
// ---------------------------------------------------------------------------

public record SearchHit(string Title, string Url, string Snippet);

// Generic web-search connector (Google Programmable Search, Bing, etc.).
public interface IWebSearchConnector
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default);
}

// SerpAPI-specific connector (kept separate so results can be parsed differently).
public interface ISerpApiConnector
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default);
}

// Microsoft Partner directory lookup.
public interface IMicrosoftPartnerConnector
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<SearchHit>> LookupAsync(string companyName, CancellationToken ct = default);
}

// Result of asking the AI to evaluate a partner. All fields optional (best effort).
public class AiSummaryResult
{
    public string? Summary { get; set; }                    // -> Partner.AiSummary
    public string? AiInfrastructureSummary { get; set; }    // -> Partner.AiInfrastructureSummary
    public List<string> SuggestedCapabilities { get; set; } = new();
    public string? SuggestedFollowUp { get; set; }          // -> Partner.FollowUpAction
    public string? Error { get; set; }                      // set when the call failed
}

// Generates the AI Summary / AI Infrastructure Summary text for a partner.
public interface IAiSummaryGenerator
{
    bool IsConfigured { get; }
    Task<AiSummaryResult> SummarizeAsync(Partner partner, CancellationToken ct = default);
}

// Weekly market radar job (planned).
public interface IMarketRadarService
{
    bool IsConfigured { get; }
    Task RunWeeklyScanAsync(CancellationToken ct = default);
}

// ---------------------------------------------------------------------------
// Default "not configured yet" implementations so dependency injection works
// out of the box. Each one simply reports IsConfigured = false.
// ---------------------------------------------------------------------------

public class NullWebSearchConnector : IWebSearchConnector, ISerpApiConnector, IMicrosoftPartnerConnector
{
    public bool IsConfigured => false;
    public Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SearchHit>>(Array.Empty<SearchHit>());
    public Task<IReadOnlyList<SearchHit>> LookupAsync(string companyName, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SearchHit>>(Array.Empty<SearchHit>());
}

public class NullAiSummaryGenerator : IAiSummaryGenerator
{
    public bool IsConfigured => false;
    public Task<AiSummaryResult> SummarizeAsync(Partner partner, CancellationToken ct = default)
        => Task.FromResult(new AiSummaryResult { Error = "AI Summary is not configured" });
}

public class NullMarketRadarService : IMarketRadarService
{
    public bool IsConfigured => false;
    public Task RunWeeklyScanAsync(CancellationToken ct = default) => Task.CompletedTask;
}
