using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PartnerFinder.Data;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

// Search Keyword Generator page. Produces copy-paste search phrases; no paid API.
public class KeywordController : Controller
{
    private readonly KeywordGeneratorService _generator;

    public KeywordController(KeywordGeneratorService generator) => _generator = generator;

    [HttpGet]
    public IActionResult Index()
    {
        PopulateDropdowns();
        // Default to US-focused research per the current MVP scope.
        return View(new KeywordRequest { Country = "United States", AiCapabilityRequired = true });
    }

    [HttpPost]
    public IActionResult Index(KeywordRequest request)
    {
        PopulateDropdowns();
        ViewBag.Keywords = _generator.Generate(request);
        return View(request);
    }

    private void PopulateDropdowns()
    {
        ViewBag.ServiceCategories = new SelectList(PartnerOptions.ServiceCategories);
    }
}
