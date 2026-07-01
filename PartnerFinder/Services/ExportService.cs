using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using PartnerFinder.Models;

namespace PartnerFinder.Services;

// Exports the partner list to CSV or Excel (.xlsx). Both use the same column order.
public class ExportService
{
    // Column header -> value selector. Single source of truth for both CSV and Excel.
    private static readonly (string Header, Func<Partner, string> Value)[] Columns =
    {
        ("Company Name", p => p.CompanyName),
        ("Country", p => p.Country ?? ""),
        ("City", p => p.City ?? ""),
        ("Website", p => p.Website ?? ""),
        ("Service Category", p => p.ServiceCategory ?? ""),
        ("Main Services", p => p.MainServices ?? ""),
        ("Contact Person", p => p.ContactPerson ?? ""),
        ("Contact Title", p => p.ContactTitle ?? ""),
        ("Email", p => p.Email ?? ""),
        ("Phone", p => p.Phone ?? ""),
        ("LinkedIn", p => p.LinkedIn ?? ""),
        ("Source URL", p => p.SourceUrl ?? ""),
        ("Notes", p => p.Notes ?? ""),
        ("Last Updated", p => p.LastUpdatedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ("Data Center Experience", p => YesNo(p.DataCenterExperience)),
        ("Smart Hands", p => YesNo(p.SmartHandsCapability)),
        ("IMAC", p => YesNo(p.ImacCapability)),
        ("Break/Fix", p => YesNo(p.BreakFixCapability)),
        ("Network Support", p => YesNo(p.NetworkSupportCapability)),
        ("Server Support", p => YesNo(p.ServerSupportCapability)),
        ("Storage Support", p => YesNo(p.StorageSupportCapability)),
        ("Microsoft Partner", p => p.MicrosoftPartnerStatus.ToString()),
        ("Dell Partner", p => p.DellPartnerStatus.ToString()),
        ("Cisco Partner", p => p.CiscoPartnerStatus.ToString()),
        ("HPE Partner", p => p.HpePartnerStatus.ToString()),
        ("Certifications", p => p.Certifications ?? ""),
        ("AI Server Build", p => YesNo(p.AiServerBuildCapability)),
        ("GPU Workstation Build", p => YesNo(p.GpuWorkstationBuildCapability)),
        ("Edge AI Deployment", p => YesNo(p.EdgeAiDeploymentCapability)),
        ("Local LLM Deployment", p => YesNo(p.LocalLlmDeploymentCapability)),
        ("NVIDIA GPU", p => YesNo(p.NvidiaGpuExperience)),
        ("AMD GPU", p => YesNo(p.AmdGpuExperience)),
        ("NVIDIA Jetson / Edge", p => YesNo(p.NvidiaJetsonExperience)),
        ("Small AI Cluster", p => YesNo(p.SmallAiClusterExperience)),
        ("On-Prem AI", p => YesNo(p.OnPremAiDeploymentExperience)),
        ("AI Inference Setup", p => YesNo(p.AiModelInferenceSetup)),
        ("Linux/Docker/K8s", p => YesNo(p.LinuxDockerKubernetesCapability)),
        ("Cooling/Power Planning", p => YesNo(p.CoolingPowerPlanningCapability)),
        ("AI Infrastructure Summary", p => p.AiInfrastructureSummary ?? ""),
        ("AI Summary", p => p.AiSummary ?? ""),
        ("Qualification Score", p => p.QualificationScore.ToString()),
        ("AI Infrastructure Score", p => p.AiInfrastructureScore.ToString()),
        ("Recommended Level", p => p.RecommendedLevel.ToString()),
        ("Manual Review Status", p => p.ManualReviewStatus.ToString()),
        ("Follow Up Action", p => p.FollowUpAction ?? ""),
    };

    public byte[] ToCsv(IEnumerable<Partner> partners)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Columns.Select(c => Escape(c.Header))));
        foreach (var p in partners)
            sb.AppendLine(string.Join(",", Columns.Select(c => Escape(c.Value(p)))));

        // UTF-8 BOM so Excel opens accented / CJK characters correctly.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(body).ToArray();
    }

    public byte[] ToExcel(IEnumerable<Partner> partners)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Partners");

        for (int c = 0; c < Columns.Length; c++)
            ws.Cell(1, c + 1).Value = Columns[c].Header;
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var p in partners)
        {
            for (int c = 0; c < Columns.Length; c++)
                ws.Cell(row, c + 1).Value = Columns[c].Value(p);
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string YesNo(bool b) => b ? "Yes" : "No";

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
