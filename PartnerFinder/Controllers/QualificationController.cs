using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartnerFinder.Data;

namespace PartnerFinder.Controllers;

// Qualification Score View: a scoreboard of all partners ranked by score, grouped by level.
public class QualificationController : Controller
{
    private readonly AppDbContext _db;

    public QualificationController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var partners = await _db.Partners.AsNoTracking()
            .OrderByDescending(p => p.QualificationScore)
            .ThenBy(p => p.CompanyName)
            .ToListAsync();
        return View(partners);
    }
}
