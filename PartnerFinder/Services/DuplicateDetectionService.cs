using PartnerFinder.Models;

namespace PartnerFinder.Services;

// Lightweight duplicate detection so the same company is not filed twice.
// Compares on a normalized company name and (when present) the website host.
// This is intentionally simple; a fuzzy-match / AI de-dup engine can replace it later.
public class DuplicateDetectionService
{
    // Returns existing partners that look like the same company as `candidate`.
    // Pass the candidate's own Id (or 0 for a new record) so it does not match itself.
    public List<Partner> FindPotentialDuplicates(Partner candidate, IEnumerable<Partner> existing)
    {
        var name = NormalizeName(candidate.CompanyName);
        var host = NormalizeHost(candidate.Website);

        return existing
            .Where(p => p.Id != candidate.Id)
            .Where(p =>
                NormalizeName(p.CompanyName) == name ||
                (host != null && NormalizeHost(p.Website) == host))
            .ToList();
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var lower = name.ToLowerInvariant();
        // Drop common company suffixes and non-alphanumerics for a stable comparison key.
        foreach (var suffix in new[] { " inc", " llc", " ltd", " corp", " co", " gmbh", " sa", " srl" })
            lower = lower.Replace(suffix, " ");
        return new string(lower.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string? NormalizeHost(string? website)
    {
        if (string.IsNullOrWhiteSpace(website)) return null;
        var w = website.Trim().ToLowerInvariant();
        if (!w.StartsWith("http")) w = "http://" + w;
        return Uri.TryCreate(w, UriKind.Absolute, out var uri)
            ? uri.Host.Replace("www.", "")
            : null;
    }
}
