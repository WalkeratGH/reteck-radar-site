namespace PartnerFinder.Models;

// Data shown on the Dashboard page.
public class DashboardViewModel
{
    public int TotalPartners { get; set; }
    public int LevelA { get; set; }
    public int LevelB { get; set; }
    public int LevelC { get; set; }
    public int AiCapableCount { get; set; }
    public int MissingContactCount { get; set; }
    public int PendingReviewCount { get; set; }
    public List<CountryCount> ByCountry { get; set; } = new();
}

public record CountryCount(string Country, int Count);

// Filters + results for the Partner List / search page.
public class PartnerListViewModel
{
    // Filters (all optional).
    public string? Query { get; set; }             // free text across name/city/services
    public string? Country { get; set; }
    public string? ServiceCategory { get; set; }
    public string? Level { get; set; }             // "A" / "B" / "C"
    public bool AiOnly { get; set; }

    public List<Partner> Partners { get; set; } = new();
    public List<string> Countries { get; set; } = new();   // for the country dropdown
}
