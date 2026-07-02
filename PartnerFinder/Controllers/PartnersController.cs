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

    public PartnersController(AppDbContext db, ScoringService scoring, DuplicateDetectionService dupes,
        WebsiteInfoService webInfo)
    {
        _db = db;
        _scoring = scoring;
        _dupes = dupes;
        _webInfo = webInfo;
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

        return View(vm);
    }

    // Partner Detail
    public async Task<IActionResult> Details(int id)
    {
        var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (partner == null) return NotFound();

        ViewBag.ScoreResult = _scoring.Score(partner);
        return View(partner);
    }

    // Add Partner (GET). Optional query params let the Web Search page pre-fill
    // the form when filing a search result as a new partner. With autoFill=true the
    // company's website is fetched and basic details (name, email, phone, LinkedIn,
    // city, description) are extracted automatically - always verify by hand.
    public async Task<IActionResult> Create(string? companyName, string? website, string? sourceUrl,
        bool autoFill = false)
    {
        PopulateDropdowns();

        var partner = new Partner
        {
            CompanyName = companyName ?? string.Empty,
            Website = website,
            SourceUrl = sourceUrl
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
