using Microsoft.EntityFrameworkCore;
using PartnerFinder.Data;
using PartnerFinder.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + views.
builder.Services.AddControllersWithViews();

// Database: SQLite via Entity Framework Core.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=partnerfinder.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Application services.
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<KeywordGeneratorService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<DuplicateDetectionService>();

// Future-expansion connectors (currently "not configured" no-op implementations).
// Swap these registrations for real implementations when you add live APIs.
builder.Services.AddSingleton<IWebSearchConnector, NullWebSearchConnector>();
builder.Services.AddSingleton<ISerpApiConnector, NullWebSearchConnector>();
builder.Services.AddSingleton<IMicrosoftPartnerConnector, NullWebSearchConnector>();
builder.Services.AddSingleton<IAiSummaryGenerator, NullAiSummaryGenerator>();
builder.Services.AddSingleton<IMarketRadarService, NullMarketRadarService>();

var app = builder.Build();

// Create/upgrade the SQLite database automatically and seed sample data on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    var scoring = scope.ServiceProvider.GetRequiredService<ScoringService>();
    DbSeeder.Seed(db, scoring);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    // Only force HTTPS in production. Running locally (e.g. on a LAN so a phone can
    // connect over plain http) skips this, keeping the console output clean.
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
