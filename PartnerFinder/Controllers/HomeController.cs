using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartnerFinder.Data;
using PartnerFinder.Models;

namespace PartnerFinder.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    // Dashboard
    public async Task<IActionResult> Index()
    {
        var partners = await _db.Partners.AsNoTracking().ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalPartners = partners.Count,
            LevelA = partners.Count(p => p.RecommendedLevel == RecommendedLevel.A),
            LevelB = partners.Count(p => p.RecommendedLevel == RecommendedLevel.B),
            LevelC = partners.Count(p => p.RecommendedLevel == RecommendedLevel.C),
            AiCapableCount = partners.Count(p => p.IsAiCapable),
            MissingContactCount = partners.Count(p => !p.HasContactInfo),
            PendingReviewCount = partners.Count(p => p.ManualReviewStatus == ManualReviewStatus.Pending),
            ByCountry = partners
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Country) ? "(Unknown)" : p.Country!)
                .Select(g => new CountryCount(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList()
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
