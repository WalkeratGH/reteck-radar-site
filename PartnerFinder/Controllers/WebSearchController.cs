using Microsoft.AspNetCore.Mvc;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

// Automatic web search (Brave Search API). Type a query, get live results,
// and file any result as a partner with one click.
public class WebSearchController : Controller
{
    private readonly IWebSearchConnector _search;

    public WebSearchController(IWebSearchConnector search) => _search = search;

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Configured = _search.IsConfigured;
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
