using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerFinder.Data;
using PartnerFinder.Models;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

public class PartnersController : Controller
{
    private readonly AppDbContext _db;
    private readonly ScoringService _scoring;
    private readonly DuplicateDetectionService _dupes;
    private readonly WebsiteInfoService _webInfo;
    private readonly IAiSummaryGenerator _ai;
    private readonly IContactFinder _contacts;

    public PartnersController(AppDbContext db, ScoringService scoring, DuplicateDetectionService dupes,
        WebsiteInfoService webInfo, IAiSummaryGenerator ai, IContactFinder contacts)
    {
        _db = db;
        _scoring = scoring;
        _dupes = dupes;
        _webInfo = webInfo;
        _ai = ai;
        _contacts = contacts;
    }

    // Partner List + search/filter
    public async Task<IActionResult> Index(string? query, string? country, string? serviceCategory,
        string? level, bool aiOnly = false)
    {
        var q = _db.Partners.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(p =>
                p.CompanyName.Contains(term) ||
                (p.City != null && p.City.Contains(term)) ||
                (p.MainServices != null && p.MainServices.Contains(term)) ||
                (p.ServiceCategory != null && p.ServiceCategory.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(country))
            q = q.Where(p => p.Country == country);

        if (!string.IsNullOrWhiteSpace(serviceCategory))
            q = q.Where(p => p.ServiceCategory == serviceCategory);

        if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse<RecommendedLevel>(level, out var lvl))
            q = q.Where(p => p.RecommendedLevel == lvl);

        if (aiOnly)
        {
            q = q.Where(p =>
                p.AiServerBuildCapability || p.GpuWorkstationBuildCapability || p.EdgeAiDeploymentCapability ||
                p.LocalLlmDeploymentCapability || p.NvidiaJetsonExperience || p.SmallAiClusterExperience ||
                p.OnPremAiDeploymentExperience || p.AiModelInferenceSetup);
        }

        var vm = new PartnerListViewModel
        {
            Query = query,
            Country = country,
            ServiceCategory = serviceCategory,
            Level = level,
            AiOnly = aiOnly,
            Partners = await q.OrderByDescending(p => p.QualificationScore)
                              .ThenBy(p => p.CompanyName)
                              .ToListAsync(),
            Countries = await _db.Partners.AsNoTracking()
                              .Where(p => p.Country != null && p.Country != "")
                              .Select(p => p.Country!)
                              .Distinct().OrderBy(c => c).ToListAsync()
        };

        ViewBag.AiConfigured = _ai.IsConfigured;
        ViewBag.AiPending = await _db.Partners.CountAsync(p => p.AiSummary == null || p.AiSummary == "");
        return View(vm);
    }

    // Partner Detail
    public async Task<IActionResult> Details(int id)
    {
        var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (partner == null) return NotFound();

        ViewBag.ScoreResult = _scoring.Score(partner);
        ViewBag.AiConfigured = _ai.IsConfigured;
        return View(partner);
    }

    // AI Research & Auto-fill: Claude reads the company's website (multiple
    // pages) plus web-search snippets and fills the record - capabilities are
    // auto-CHECKED when evidence supports them (never unchecked), text fields
    // are filled only when empty, and the record is re-scored. The AI Summary
    // lists everything that was auto-applied so a human can verify.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAiSummary(int id)
    {
        var partner = await _db.Partners.FindAsync(id);
        if (partner == null) return NotFound();

        var result = await _ai.SummarizeAsync(partner);
        if (result.Error != null)
        {
            TempData["DuplicateWarning"] = $"AI research failed: {result.Error}";
            return RedirectToAction(nameof(Details), new { id });
        }

        ApplyResearch(partner, result);
        await EnrichContactsAsync(partner);
        await _db.SaveChangesAsync();

        TempData["Message"] = "AI research complete - fields were auto-filled, please verify below.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Research up to 5 partners that have no AI Summary yet (batch takes a
    // couple of minutes because each company is researched individually).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResearchBatch()
    {
        var pending = await _db.Partners
            .Where(p => p.AiSummary == null || p.AiSummary == "")
            .OrderBy(p => p.Id)
            .Take(5)
            .ToListAsync();

        if (pending.Count == 0)
        {
            TempData["Message"] = "All partners already have an AI summary.";
            return RedirectToAction(nameof(Index));
        }

        int ok = 0;
        var failures = new List<string>();
        foreach (var partner in pending)
        {
            var result = await _ai.SummarizeAsync(partner);
            if (result.Error != null)
            {
                failures.Add($"{partner.CompanyName}: {result.Error}");
                continue;
            }
            ApplyResearch(partner, result);
            await EnrichContactsAsync(partner);
            await _db.SaveChangesAsync();
            ok++;
        }

        var remaining = await _db.Partners.CountAsync(p => p.AiSummary == null || p.AiSummary == "");
        TempData["Message"] = $"AI research finished for {ok} partner(s). {remaining} still pending - click again to continue.";
        if (failures.Count > 0)
            TempData["DuplicateWarning"] = string.Join(" | ", failures.Take(3));
        return RedirectToAction(nameof(Index));
    }

    // One click from Web Search / Discover: file the company AND run AI research
    // on it. Because that work takes ~30-60s, this GET returns an instant
    // "processing" page with a spinner; its JavaScript then calls
    // FileAndResearchRun (below) to do the actual work and redirects to the
    // finished record. Opening it in a new tab therefore shows progress
    // immediately instead of a blank page.
    [HttpGet]
    public IActionResult FileAndResearch(string companyName, string website, string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(website)) return RedirectToAction(nameof(Index));
        ViewBag.CompanyName = companyName;
        ViewBag.Website = website;
        ViewBag.SourceUrl = sourceUrl;
        return View("Researching");
    }

    // Does the actual filing + AI research (called by the Researching page via
    // fetch). Returns JSON with the URL of the resulting record so the browser
    // can navigate to it once the work finishes.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FileAndResearchRun(string companyName, string website, string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(website))
            return Json(new { error = "No website was provided." });

        // If this company is already filed, go to the existing record.
        var candidate = new Partner { CompanyName = companyName, Website = website };
        var existing = await _db.Partners.AsNoTracking().ToListAsync();
        var dupe = _dupes.FindPotentialDuplicates(candidate, existing).FirstOrDefault();
        if (dupe != null)
        {
            TempData["Message"] = $"\"{dupe.CompanyName}\" was already filed - showing the existing record.";
            return Json(new { url = Url.Action(nameof(Details), new { id = dupe.Id }) });
        }

        // Quick pre-fill from the website, then create the record.
        var partner = new Partner { CompanyName = companyName, Website = website, SourceUrl = sourceUrl };
        var info = await _webInfo.FetchAsync(website);
        if (info.Error == null)
        {
            if (!string.IsNullOrWhiteSpace(info.CompanyName)) partner.CompanyName = info.CompanyName!;
            partner.MainServices = info.Description;
            partner.Email = info.Email;
            partner.Phone = info.Phone;
            partner.LinkedIn = info.LinkedIn;
            partner.City = info.City;
            partner.Country = info.Country;
        }
        partner.LastUpdatedDate = DateTime.UtcNow;
        _scoring.Apply(partner);
        _db.Partners.Add(partner);
        await _db.SaveChangesAsync();

        // Deep research + auto-fill.
        var result = await _ai.SummarizeAsync(partner);
        if (result.Error != null)
        {
            TempData["DuplicateWarning"] = $"Filed, but AI research failed: {result.Error}";
            return Json(new { url = Url.Action(nameof(Details), new { id = partner.Id }) });
        }
        ApplyResearch(partner, result);
        await EnrichContactsAsync(partner);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"\"{partner.CompanyName}\" filed and researched - please verify the auto-filled fields.";
        return Json(new { url = Url.Action(nameof(Details), new { id = partner.Id }) });
    }

    // Fills Email / Contact Person / Contact Title from Hunter.io when they are
    // still empty after AI research. Best effort - failures are ignored.
    private async Task EnrichContactsAsync(Partner p)
    {
        if (!_contacts.IsConfigured || string.IsNullOrWhiteSpace(p.Website)) return;
        if (!string.IsNullOrWhiteSpace(p.Email) &&
            !string.IsNullOrWhiteSpace(p.ContactPerson) &&
            !string.IsNullOrWhiteSpace(p.ContactTitle)) return;

        var found = await _contacts.FindAsync(p.Website!);
        if (found.Error != null) return;

        if (string.IsNullOrWhiteSpace(p.Email)) p.Email = found.Email;
        if (string.IsNullOrWhiteSpace(p.ContactPerson)) p.ContactPerson = found.ContactPerson;
        if (string.IsNullOrWhiteSpace(p.ContactTitle)) p.ContactTitle = found.ContactTitle;
        _scoring.Apply(p);
    }

    // Copies an AI research result onto the entity: text fields fill-if-empty,
    // capabilities/brands/signals are only ever turned ON, then re-score.
    private void ApplyResearch(Partner p, AiSummaryResult r)
    {
        if (string.IsNullOrWhiteSpace(p.MainServices)) p.MainServices = r.MainServices;
        if (string.IsNullOrWhiteSpace(p.Certifications)) p.Certifications = r.Certifications;
        if (string.IsNullOrWhiteSpace(p.City)) p.City = r.City;
        if (string.IsNullOrWhiteSpace(p.Country)) p.Country = r.Country;
        if (string.IsNullOrWhiteSpace(p.Email)) p.Email = r.Email;
        if (string.IsNullOrWhiteSpace(p.Phone)) p.Phone = r.Phone;
        if (string.IsNullOrWhiteSpace(p.AiInfrastructureSummary)) p.AiInfrastructureSummary = r.AiInfrastructureSummary;
        if (string.IsNullOrWhiteSpace(p.FollowUpAction)) p.FollowUpAction = r.SuggestedFollowUp;

        var newlyChecked = new List<string>();
        foreach (var name in r.SuggestedCapabilities.Distinct())
            if (SetCapability(p, name)) newlyChecked.Add(name);

        var newBrands = new List<string>();
        foreach (var brand in r.BrandPartnerships.Distinct())
        {
            switch (brand)
            {
                case "Microsoft" when p.MicrosoftPartnerStatus == PartnerStatus.None:
                    p.MicrosoftPartnerStatus = PartnerStatus.Registered; newBrands.Add(brand); break;
                case "Dell" when p.DellPartnerStatus == PartnerStatus.None:
                    p.DellPartnerStatus = PartnerStatus.Registered; newBrands.Add(brand); break;
                case "Cisco" when p.CiscoPartnerStatus == PartnerStatus.None:
                    p.CiscoPartnerStatus = PartnerStatus.Registered; newBrands.Add(brand); break;
                case "HPE" when p.HpePartnerStatus == PartnerStatus.None:
                    p.HpePartnerStatus = PartnerStatus.Registered; newBrands.Add(brand); break;
            }
        }

        if (r.EquipmentLeasing == true) p.EquipmentLeasingSignal = true;
        if (r.SmeFocus == true) p.SmeFocusSignal = true;

        var summary = new System.Text.StringBuilder(r.Summary ?? "");
        if (newlyChecked.Count > 0)
            summary.Append($"\nAuto-checked from evidence (please verify): {string.Join(", ", newlyChecked)}.");
        if (newBrands.Count > 0)
            summary.Append($"\nBrand partnerships found (set to Registered): {string.Join(", ", newBrands)}.");
        p.AiSummary = summary.ToString();

        p.LastUpdatedDate = DateTime.UtcNow;
        _scoring.Apply(p);
    }

    // Maps a capability name from the AI to the matching bool field.
    // Returns true only when the flag flipped from off to on.
    private static bool SetCapability(Partner p, string name)
    {
        (Func<bool> get, Action set)? f = name switch
        {
            "Data Center Experience" => (() => p.DataCenterExperience, () => p.DataCenterExperience = true),
            "Smart Hands" => (() => p.SmartHandsCapability, () => p.SmartHandsCapability = true),
            "IMAC" => (() => p.ImacCapability, () => p.ImacCapability = true),
            "Break/Fix" => (() => p.BreakFixCapability, () => p.BreakFixCapability = true),
            "Network Support" => (() => p.NetworkSupportCapability, () => p.NetworkSupportCapability = true),
            "Server Support" => (() => p.ServerSupportCapability, () => p.ServerSupportCapability = true),
            "Storage Support" => (() => p.StorageSupportCapability, () => p.StorageSupportCapability = true),
            "AI Server Build" => (() => p.AiServerBuildCapability, () => p.AiServerBuildCapability = true),
            "GPU Workstation Build" => (() => p.GpuWorkstationBuildCapability, () => p.GpuWorkstationBuildCapability = true),
            "Edge AI Deployment" => (() => p.EdgeAiDeploymentCapability, () => p.EdgeAiDeploymentCapability = true),
            "Local LLM Deployment" => (() => p.LocalLlmDeploymentCapability, () => p.LocalLlmDeploymentCapability = true),
            "NVIDIA GPU Experience" => (() => p.NvidiaGpuExperience, () => p.NvidiaGpuExperience = true),
            "AMD GPU Experience" => (() => p.AmdGpuExperience, () => p.AmdGpuExperience = true),
            "NVIDIA Jetson / Edge Device" => (() => p.NvidiaJetsonExperience, () => p.NvidiaJetsonExperience = true),
            "Small AI Cluster" => (() => p.SmallAiClusterExperience, () => p.SmallAiClusterExperience = true),
            "On-Prem AI Deployment" => (() => p.OnPremAiDeploymentExperience, () => p.OnPremAiDeploymentExperience = true),
            "AI Model Inference Setup" => (() => p.AiModelInferenceSetup, () => p.AiModelInferenceSetup = true),
            "Linux/Docker/Kubernetes" => (() => p.LinuxDockerKubernetesCapability, () => p.LinuxDockerKubernetesCapability = true),
            "Cooling/Power Planning" => (() => p.CoolingPowerPlanningCapability, () => p.CoolingPowerPlanningCapability = true),
            _ => null,
        };
        if (f == null || f.Value.get()) return false;
        f.Value.set();
        return true;
    }

    // Add Partner (GET). Optional query params let the Web Search page pre-fill
    // the form when filing a search result as a new partner. With autoFill=true the
    // company's website is fetched and basic details (name, email, phone, LinkedIn,
    // city, description) are extracted automatically - always verify by hand.
    public async Task<IActionResult> Create(string? companyName, string? website, string? sourceUrl,
        bool autoFill = false, string? city = null, string? country = null)
    {
        PopulateDropdowns();

        var partner = new Partner
        {
            CompanyName = companyName ?? string.Empty,
            Website = website,
            SourceUrl = sourceUrl,
            City = city,
            Country = country
        };

        if (autoFill && !string.IsNullOrWhiteSpace(website))
        {
            var info = await _webInfo.FetchAsync(website);
            if (info.Error != null)
            {
                ViewBag.AutoFillNote = $"Auto-fill: could not read the website ({info.Error}). Please fill in manually.";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(info.CompanyName)) partner.CompanyName = info.CompanyName!;
                partner.MainServices ??= info.Description;
                partner.Email ??= info.Email;
                partner.Phone ??= info.Phone;
                partner.LinkedIn ??= info.LinkedIn;
                partner.City ??= info.City;
                partner.Country ??= info.Country;

                var found = string.Join(", ", info.FoundFields());
                ViewBag.AutoFillNote = found.Length > 0
                    ? $"Auto-filled from the company website: {found}. Please verify before saving."
                    : "Auto-fill: the website had no readable contact details. Please fill in manually.";
            }
        }

        // Form inputs read ModelState (bound from the query string) before the model,
        // which would show the raw companyName param instead of the auto-filled name.
        ModelState.Clear();
        return View(partner);
    }

    // Add Partner (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Partner partner)
    {
        if (!ModelState.IsValid)
        {
            PopulateDropdowns();
            return View(partner);
        }

        // Warn about likely duplicates (does not block saving).
        var existing = await _db.Partners.AsNoTracking().ToListAsync();
        var dupes = _dupes.FindPotentialDuplicates(partner, existing);
        if (dupes.Any())
            TempData["DuplicateWarning"] =
                $"Heads up: {dupes.Count} existing partner(s) look similar (e.g. \"{dupes[0].CompanyName}\").";

        partner.LastUpdatedDate = DateTime.UtcNow;
        _scoring.Apply(partner);
        _db.Partners.Add(partner);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Added \"{partner.CompanyName}\".";
        return RedirectToAction(nameof(Details), new { id = partner.Id });
    }

    // Edit Partner (GET)
    public async Task<IActionResult> Edit(int id)
    {
        var partner = await _db.Partners.FindAsync(id);
        if (partner == null) return NotFound();
        PopulateDropdowns();
        return View(partner);
    }

    // Edit Partner (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Partner partner)
    {
        if (id != partner.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            PopulateDropdowns();
            return View(partner);
        }

        partner.LastUpdatedDate = DateTime.UtcNow;
        _scoring.Apply(partner);
        _db.Partners.Update(partner);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Saved \"{partner.CompanyName}\".";
        return RedirectToAction(nameof(Details), new { id = partner.Id });
    }

    // Delete Partner (GET - confirmation)
    public async Task<IActionResult> Delete(int id)
    {
        var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (partner == null) return NotFound();
        return View(partner);
    }

    // Delete Partner (POST)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var partner = await _db.Partners.FindAsync(id);
        if (partner != null)
        {
            _db.Partners.Remove(partner);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Deleted \"{partner.CompanyName}\".";
        }
        return RedirectToAction(nameof(Index));
    }

    // Quick update of review status / follow-up from the Detail page.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReview(int id, ManualReviewStatus manualReviewStatus, string? followUpAction)
    {
        var partner = await _db.Partners.FindAsync(id);
        if (partner == null) return NotFound();

        partner.ManualReviewStatus = manualReviewStatus;
        partner.FollowUpAction = followUpAction;
        partner.LastUpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Review status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private void PopulateDropdowns()
    {
        ViewBag.ServiceCategories = new SelectList(PartnerOptions.ServiceCategories);
    }
}
