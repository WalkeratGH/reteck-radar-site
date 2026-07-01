using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartnerFinder.Data;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

// Settings page: shows environment info, the status of future connectors, and the
// service-category list. Read-only in the MVP - it documents how to extend the system.
public class SettingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebSearchConnector _webSearch;
    private readonly ISerpApiConnector _serpApi;
    private readonly IMicrosoftPartnerConnector _msPartner;
    private readonly IAiSummaryGenerator _aiSummary;
    private readonly IMarketRadarService _marketRadar;

    public SettingsController(
        AppDbContext db,
        IWebSearchConnector webSearch,
        ISerpApiConnector serpApi,
        IMicrosoftPartnerConnector msPartner,
        IAiSummaryGenerator aiSummary,
        IMarketRadarService marketRadar)
    {
        _db = db;
        _webSearch = webSearch;
        _serpApi = serpApi;
        _msPartner = msPartner;
        _aiSummary = aiSummary;
        _marketRadar = marketRadar;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.PartnerCount = await _db.Partners.CountAsync();
        ViewBag.DatabasePath = _db.Database.GetConnectionString();
        ViewBag.Connectors = new (string Name, bool Configured)[]
        {
            ("Web Search API connector", _webSearch.IsConfigured),
            ("SerpAPI connector", _serpApi.IsConfigured),
            ("Microsoft Partner directory connector", _msPartner.IsConfigured),
            ("AI Summary generator", _aiSummary.IsConfigured),
            ("Weekly market radar", _marketRadar.IsConfigured),
        };
        ViewBag.ServiceCategories = PartnerOptions.ServiceCategories;
        return View();
    }
}
