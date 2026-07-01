using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartnerFinder.Data;
using PartnerFinder.Services;

namespace PartnerFinder.Controllers;

// Export CSV / Excel page and download endpoints.
public class ExportController : Controller
{
    private readonly AppDbContext _db;
    private readonly ExportService _export;

    public ExportController(AppDbContext db, ExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Count = await _db.Partners.CountAsync();
        return View();
    }

    public async Task<IActionResult> Csv()
    {
        var partners = await _db.Partners.AsNoTracking().OrderBy(p => p.CompanyName).ToListAsync();
        var bytes = _export.ToCsv(partners);
        return File(bytes, "text/csv", $"partners-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> Excel()
    {
        var partners = await _db.Partners.AsNoTracking().OrderBy(p => p.CompanyName).ToListAsync();
        var bytes = _export.ToExcel(partners);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"partners-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
