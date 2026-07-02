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
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Configured = _search.IsConfigured;
        ViewBag.AiConfigured = _ai.IsConfigured;
        ViewBag.Query = q;

        if (_search.IsConfigured && !string.IsNullOrWhiteSpace(q))
        {
            try
            {
                ViewBag.Results = await _search.SearchAsync(q);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }
        }

        return View();
    }
}
