using System.Net;
using System.Text.RegularExpressions;

namespace PartnerFinder.Services;

// What we managed to extract from a company website. All fields are optional -
// this is best-effort: only information published on the page can be found.
public class WebsiteInfo
{
    public string? CompanyName { get; set; }
    public string? Description { get; set; }   // meta description -> Main Services draft
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedIn { get; set; }
    public string? City { get; set; }          // "Dallas, TX" style when a US address is found
    public string? Country { get; set; }
    public string? Error { get; set; }         // set when the fetch failed

    public IEnumerable<string> FoundFields()
    {
        if (!string.IsNullOrWhiteSpace(CompanyName)) yield return "Company Name";
        if (!string.IsNullOrWhiteSpace(Description)) yield return "Main Services";
        if (!string.IsNullOrWhiteSpace(Email)) yield return "Email";
        if (!string.IsNullOrWhiteSpace(Phone)) yield return "Phone";
        if (!string.IsNullOrWhiteSpace(LinkedIn)) yield return "LinkedIn";
        if (!string.IsNullOrWhiteSpace(City)) yield return "City";
    }
}

// Fetches a company's public website (homepage + contact/about page, max 2 requests)
// and extracts basic details: company name, description, email, phone, LinkedIn,
// and a US city/state when an address is visible. Used to pre-fill the Add Partner
// form when filing a Web Search result. Always verify manually afterwards.
public class WebsiteInfoService
{
    private const int MaxHtmlChars = 500_000;

    private static readonly Regex TitleRx = new(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex OgSiteNameRx = new(@"<meta[^>]+property\s*=\s*[""']og:site_name[""'][^>]+content\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MetaDescRx = new(@"<meta[^>]+name\s*=\s*[""']description[""'][^>]+content\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MetaDescRx2 = new(@"<meta[^>]+content\s*=\s*[""']([^""']+)[""'][^>]+name\s*=\s*[""']description[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MailtoRx = new(@"mailto:([a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EmailRx = new(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex TelRx = new(@"tel:([+0-9().\s\-%]{7,30})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UsPhoneRx = new(@"\(?\b\d{3}\)?[\s.\-]\d{3}[\s.\-]\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex LinkedInRx = new(@"https?://(?:www\.)?linkedin\.com/(?:company|in)/[A-Za-z0-9\-_.%]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ContactHrefRx = new(@"href\s*=\s*[""']([^""']*(?:contact|about)[^""']*)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UsAddressRx = new(
        @"([A-Z][A-Za-z .'\-]{2,30}),\s*(AL|AK|AZ|AR|CA|CO|CT|DE|FL|GA|HI|ID|IL|IN|IA|KS|KY|LA|ME|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|OH|OK|OR|PA|RI|SC|SD|TN|TX|UT|VT|VA|WA|WV|WI|WY|DC)\s+\d{5}",
        RegexOptions.Compiled);

    // File extensions and domains that produce junk "email" matches in HTML.
    private static readonly string[] JunkEmailBits =
        { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".css", ".js", "example.com", "sentry", "wixpress", "@2x", "domain.com", "email.com", "yourcompany" };

    private readonly HttpClient _http;

    public WebsiteInfoService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(12);
        // Browser-like headers: many company sites (Cloudflare etc.) return 403 to
        // anything that looks like a bot, so we present ourselves as a normal browser.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    public async Task<WebsiteInfo> FetchAsync(string website, CancellationToken ct = default)
    {
        var info = new WebsiteInfo();

        var url = website.Trim();
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            info.Error = "Invalid website URL";
            return info;
        }

        string html;
        try
        {
            html = Truncate(await _http.GetStringAsync(uri, ct));
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            // e.g. 403 = the site blocks automated access; 404 = wrong URL.
            info.Error = $"the website answered HTTP {(int)ex.StatusCode.Value} {ex.StatusCode}";
            return info;
        }
        catch (TaskCanceledException)
        {
            info.Error = "the website took too long to respond (timeout)";
            return info;
        }
        catch (Exception ex)
        {
            info.Error = $"could not reach the website ({ex.GetType().Name})";
            return info;
        }

        Extract(html, uri, info);

        // If contact details are still missing, try one contact/about page.
        if (info.Email == null || info.Phone == null)
        {
            var contactUrl = FindContactUrl(html, uri);
            if (contactUrl != null)
            {
                try
                {
                    var contactHtml = Truncate(await _http.GetStringAsync(contactUrl, ct));
                    Extract(contactHtml, uri, info);
                }
                catch
                {
                    // Contact page is a bonus; ignore failures.
                }
            }
        }

        return info;
    }

    private static string Truncate(string html)
        => html.Length > MaxHtmlChars ? html[..MaxHtmlChars] : html;

    // ------------------------------------------------------------------
    // Deep text extraction for AI research: fetches the homepage plus up to
    // 3 relevant internal pages (about/services/products/...) and returns
    // their readable text, capped so the AI prompt stays a reasonable size.
    // ------------------------------------------------------------------
    private const int MaxTextPerPage = 8_000;
    private const int MaxPagesToFollow = 3;

    private static readonly Regex ScriptStyleRx = new(@"<(script|style|noscript|svg)[\s\S]*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagRx = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex AnyHrefRx = new(@"href\s*=\s*[""']([^""'#]+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InterestingPathRx = new(
        @"about|service|solution|product|company|capabilit|offering|industri|ai|gpu|data-?cent|leas|rental|smb|sme",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<string> FetchSiteTextAsync(string website, CancellationToken ct = default)
    {
        var url = website.Trim();
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var home)) return string.Empty;

        var sb = new System.Text.StringBuilder();

        string homeHtml;
        try
        {
            homeHtml = Truncate(await _http.GetStringAsync(home, ct));
        }
        catch
        {
            return string.Empty;
        }

        sb.AppendLine($"=== Page: {home} ===");
        sb.AppendLine(HtmlToText(homeHtml));

        // Follow a few promising internal links (about/services/products/...).
        var followed = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { home.AbsolutePath };
        foreach (Match m in AnyHrefRx.Matches(homeHtml))
        {
            if (followed >= MaxPagesToFollow) break;
            var href = m.Groups[1].Value;
            if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) continue;
            if (!Uri.TryCreate(home, href, out var abs) || abs.Host != home.Host) continue;
            if (!InterestingPathRx.IsMatch(abs.AbsolutePath)) continue;
            if (!seen.Add(abs.AbsolutePath)) continue;

            try
            {
                var html = Truncate(await _http.GetStringAsync(abs, ct));
                sb.AppendLine();
                sb.AppendLine($"=== Page: {abs} ===");
                sb.AppendLine(HtmlToText(html));
                followed++;
            }
            catch
            {
                // Skip pages that fail; the homepage text is usually enough.
            }
        }

        return sb.ToString();
    }

    private static string HtmlToText(string html)
    {
        var cleaned = ScriptStyleRx.Replace(html, " ");
        cleaned = TagRx.Replace(cleaned, " ");
        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned.Length > MaxTextPerPage ? cleaned[..MaxTextPerPage] : cleaned;
    }

    private static void Extract(string html, Uri site, WebsiteInfo info)
    {
        info.CompanyName ??= ExtractCompanyName(html);
        info.Description ??= ExtractDescription(html);
        info.Email ??= ExtractEmail(html, site);
        info.Phone ??= ExtractPhone(html);
        info.LinkedIn ??= LinkedInRx.Match(html) is { Success: true } li ? li.Value : null;

        if (info.City == null && UsAddressRx.Match(html) is { Success: true } addr)
        {
            info.City = $"{addr.Groups[1].Value.Trim()}, {addr.Groups[2].Value}";
            info.Country = "United States";
        }
    }

    private static string? ExtractCompanyName(string html)
    {
        var og = OgSiteNameRx.Match(html);
        if (og.Success) return Clean(og.Groups[1].Value);

        var title = TitleRx.Match(html);
        if (!title.Success) return null;

        // Titles are usually "Brand | tagline" or "Page - Brand"; pick the shortest
        // plausible segment as the brand name.
        var segments = Clean(title.Groups[1].Value)
            .Split(new[] { '|', '–', '—', '·' }, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(s => s.Contains(" - ") ? s.Split(" - ", StringSplitOptions.RemoveEmptyEntries) : new[] { s })
            .Select(s => s.Trim())
            .Where(s => s.Length is >= 2 and <= 60 && s.Split(' ').Length <= 5)
            .ToList();

        return segments.Count > 0 ? segments.OrderBy(s => s.Length).First() : null;
    }

    private static string? ExtractDescription(string html)
    {
        var m = MetaDescRx.Match(html);
        if (!m.Success) m = MetaDescRx2.Match(html);
        if (!m.Success) return null;
        var desc = Clean(m.Groups[1].Value);
        return desc.Length > 500 ? desc[..500] : desc;
    }

    private static string? ExtractEmail(string html, Uri site)
    {
        var candidates = MailtoRx.Matches(html).Select(m => m.Groups[1].Value)
            .Concat(EmailRx.Matches(html).Select(m => m.Value))
            .Select(e => e.Trim().TrimEnd('.'))
            .Where(e => !JunkEmailBits.Any(j => e.Contains(j, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0) return null;

        // Prefer an address on the company's own domain (info@company.com).
        var host = site.Host.Replace("www.", "");
        return candidates.FirstOrDefault(e => e.EndsWith("@" + host, StringComparison.OrdinalIgnoreCase) ||
                                              e.EndsWith("." + host, StringComparison.OrdinalIgnoreCase))
               ?? candidates[0];
    }

    private static string? ExtractPhone(string html)
    {
        var tel = TelRx.Match(html);
        if (tel.Success)
        {
            // Uri.UnescapeDataString keeps a literal "+" (UrlDecode would turn it into a space).
            var t = Uri.UnescapeDataString(tel.Groups[1].Value).Trim();
            if (t.Count(char.IsDigit) >= 7) return t;
        }
        var us = UsPhoneRx.Match(html);
        return us.Success ? us.Value : null;
    }

    private static Uri? FindContactUrl(string html, Uri site)
    {
        foreach (Match m in ContactHrefRx.Matches(html))
        {
            var href = m.Groups[1].Value;
            if (href.StartsWith("#") || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) continue;
            if (Uri.TryCreate(site, href, out var abs) && abs.Host == site.Host)
                return abs;
        }
        return null;
    }

    private static string Clean(string s)
        => WebUtility.HtmlDecode(Regex.Replace(s, @"\s+", " ")).Trim();
}
