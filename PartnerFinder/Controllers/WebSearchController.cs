using Microsoft.AspNetCore.Mvc;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

// Automatic web search (Brave Search API). Type a query, get live results,
// and file any result as a partner with one click.
public class WebSearchController : Controller
{
    private readonly IWebSearchConnector _search;
    private readonly IAiSummaryGenerator _ai;

    public WebSearchController(IWebSearchConnector search, IAiSummaryGenerator ai)
    {
        _search = search;
        _ai = ai;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 0)
    {
        if (page < 0) page = 0;
        if (page > BraveWebSearchConnector.MaxOffset) page = BraveWebSearchConnector.MaxOffset;

        ViewBag.Configured = _search.IsConfigured;
        ViewBag.AiConfigured = _ai.IsConfigured;
        ViewBag.Query = q;
        ViewBag.Page = page;

        if (_search.IsConfigured && !string.IsNullOrWhiteSpace(q))
        {
            try
            {
                var results = await _search.SearchAsync(q, page);
                ViewBag.Results = results;
                // Brave doesn't return a total count, so offer "Next" whenever a
                // full page came back (and we're below Brave's page-offset cap).
                ViewBag.HasPrevious = page > 0;
                ViewBag.HasNext = results.Count >= BraveWebSearchConnector.PageSize
                                  && page < BraveWebSearchConnector.MaxOffset;
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }
        }

        return View();
    }
}
