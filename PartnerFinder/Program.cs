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

// Live web search via Brave Search API. Activates automatically once an API key
// is set in appsettings.json ("Search:Brave:ApiKey"); otherwise IsConfigured is
// false and the Web Search page shows setup instructions instead of results.
builder.Services.AddHttpClient<IWebSearchConnector, BraveWebSearchConnector>();

// Fetches a company website to pre-fill partner details (auto-enrichment).
// Automatic decompression makes requests look like a normal browser (gzip/br).
builder.Services.AddHttpClient<WebsiteInfoService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

// Other future-expansion connectors (still "not configured" no-op implementations).
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
