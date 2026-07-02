using Microsoft.AspNetCore.Mvc;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

// Discover page: search the SAM.gov federal-contractor registry (official free
// API) for US IT service providers by state + industry code, then file the
// interesting ones as partners with one click.
public class DiscoverController : Controller
{
    private readonly SamGovService _sam;
    private readonly IAiSummaryGenerator _ai;

    public DiscoverController(SamGovService sam, IAiSummaryGenerator ai)
    {
        _sam = sam;
        _ai = ai;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? state, string? naics, string? name, int page = 0)
    {
        ViewBag.Configured = _sam.IsConfigured;
        ViewBag.AiConfigured = _ai.IsConfigured;
        ViewBag.State = state;
        ViewBag.Naics = naics;
        ViewBag.Name = name;
        ViewBag.NaicsOptions = SamGovService.NaicsOptions;

        if (_sam.IsConfigured &&
            (!string.IsNullOrWhiteSpace(state) || !string.IsNullOrWhiteSpace(naics) || !string.IsNullOrWhiteSpace(name)))
        {
            ViewBag.Result = await _sam.SearchAsync(state, naics, name, page);
        }

        return View();
    }
}
