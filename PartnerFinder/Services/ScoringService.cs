using PartnerFinder.Models;

namespace PartnerFinder.Services;

// One line item of the score breakdown, used to explain *why* a partner got its score.
public record ScoreLine(string Group, string Item, int Points, int MaxPoints);

// Full result of scoring a partner.
public class ScoreResult
{
    public int Total { get; set; }              // 0-100  -> Partner.QualificationScore
    public int GeneralScore { get; set; }       // 0-70   (IT / SI portion)
    public int AiScore { get; set; }            // 0-30   -> Partner.AiInfrastructureScore
    public RecommendedLevel Level { get; set; }
    public List<ScoreLine> Lines { get; } = new();
}

// ---------------------------------------------------------------------------
// Partner Qualification Scoring Model (100 points).
//
//  General IT / SI capability .......................... 70
//    - Data Center / Enterprise IT experience .......... 20
//    - Local coverage capability ....................... 10
//    - Brand partnership (MS / Dell / Cisco / HPE) ..... 10
//    - Certification and compliance .................... 10
//    - Service scope match ............................. 10
//    - Contact availability ............................  5
//    - Website / public information credibility ........  5
//
//  AI Infrastructure capability ........................ 30
//    - AI server / GPU workstation deployment .......... 10
//    - Edge AI / local AI deployment ...................  8
//    - Linux / Docker / Kubernetes .....................  6
//    - Cooling / power planning (small AI computing) ...  4
//    - NVIDIA / AMD GPU ecosystem ......................  2
//
//  Grading:  A = 80-100,  B = 60-79,  C = below 60
// ---------------------------------------------------------------------------
public class ScoringService
{
    public ScoreResult Score(Partner p)
    {
        var r = new ScoreResult();

        // -------- General IT / SI (70) --------

        // 1. Data Center / Enterprise IT experience (20)
        int dc = 0;
        if (p.DataCenterExperience) dc += 10;
        if (p.ServerSupportCapability) dc += 4;
        if (p.StorageSupportCapability) dc += 3;
        if (p.NetworkSupportCapability) dc += 3;
        dc = Math.Min(dc, 20);
        Add(r, "General", "Data Center / Enterprise IT experience", dc, 20);

        // 2. Local coverage capability (10)
        int local = 0;
        if (p.SmartHandsCapability) local += 4;
        if (p.ImacCapability) local += 3;
        if (p.BreakFixCapability) local += 3;
        local = Math.Min(local, 10);
        Add(r, "General", "Local coverage capability", local, 10);

        // 3. Brand partnership (10) - 3 points per active brand, capped at 10
        int brands = 0;
        if (p.MicrosoftPartnerStatus != PartnerStatus.None) brands++;
        if (p.DellPartnerStatus != PartnerStatus.None) brands++;
        if (p.CiscoPartnerStatus != PartnerStatus.None) brands++;
        if (p.HpePartnerStatus != PartnerStatus.None) brands++;
        int brandScore = Math.Min(brands * 3, 10);
        Add(r, "General", "Brand partnership (MS / Dell / Cisco / HPE)", brandScore, 10);

        // 4. Certification and compliance (10)
        int cert = 0;
        var certs = (p.Certifications ?? string.Empty).ToLowerInvariant();
        if (certs.Contains("27001")) cert += 5;
        if (certs.Contains("9001")) cert += 5;
        // Any other certification text still earns partial credit.
        if (cert == 0 && !string.IsNullOrWhiteSpace(p.Certifications)) cert = 4;
        cert = Math.Min(cert, 10);
        Add(r, "General", "Certification and compliance", cert, 10);

        // 5. Service scope match (10)
        int scope = 0;
        if (!string.IsNullOrWhiteSpace(p.ServiceCategory)) scope += 5;
        if (!string.IsNullOrWhiteSpace(p.MainServices)) scope += 5;
        Add(r, "General", "Service scope match", scope, 10);

        // 6. Contact availability (5)
        int contact = 0;
        if (!string.IsNullOrWhiteSpace(p.Email)) contact += 3;
        if (!string.IsNullOrWhiteSpace(p.Phone)) contact += 2;
        Add(r, "General", "Contact availability", contact, 5);

        // 7. Website / public information credibility (5)
        int web = 0;
        if (!string.IsNullOrWhiteSpace(p.Website)) web += 3;
        if (!string.IsNullOrWhiteSpace(p.LinkedIn)) web += 1;
        if (!string.IsNullOrWhiteSpace(p.SourceUrl)) web += 1;
        web = Math.Min(web, 5);
        Add(r, "General", "Website / public information credibility", web, 5);

        r.GeneralScore = dc + local + brandScore + cert + scope + contact + web;

        // -------- AI Infrastructure (30) --------

        // 8. AI server / GPU workstation deployment experience (10)
        int aiBuild = 0;
        if (p.AiServerBuildCapability) aiBuild += 5;
        if (p.GpuWorkstationBuildCapability) aiBuild += 5;
        Add(r, "AI", "AI server / GPU workstation deployment", aiBuild, 10);

        // 9. Edge AI / local AI deployment experience (8)
        int edge = 0;
        if (p.EdgeAiDeploymentCapability) edge += 3;
        if (p.LocalLlmDeploymentCapability) edge += 3;
        if (p.OnPremAiDeploymentExperience) edge += 2;
        edge = Math.Min(edge, 8);
        Add(r, "AI", "Edge AI / local AI deployment", edge, 8);

        // 10. Linux / Docker / Kubernetes capability (6)
        int k8s = p.LinuxDockerKubernetesCapability ? 6 : 0;
        Add(r, "AI", "Linux / Docker / Kubernetes capability", k8s, 6);

        // 11. Cooling / power planning for small AI computing (4)
        int cooling = p.CoolingPowerPlanningCapability ? 4 : 0;
        Add(r, "AI", "Cooling / power planning", cooling, 4);

        // 12. NVIDIA / AMD GPU ecosystem experience (2)
        int gpu = 0;
        if (p.NvidiaGpuExperience) gpu += 1;
        if (p.AmdGpuExperience) gpu += 1;
        Add(r, "AI", "NVIDIA / AMD GPU ecosystem", gpu, 2);

        r.AiScore = aiBuild + edge + k8s + cooling + gpu;

        // -------- Total & grade --------
        r.Total = r.GeneralScore + r.AiScore;
        r.Level = GradeFor(r.Total);
        return r;
    }

    // Score the partner and copy the results back onto the entity so they persist.
    public ScoreResult Apply(Partner p)
    {
        var r = Score(p);
        p.QualificationScore = r.Total;
        p.AiInfrastructureScore = r.AiScore;
        p.RecommendedLevel = r.Level;
        return r;
    }

    public static RecommendedLevel GradeFor(int total)
    {
        if (total >= 80) return RecommendedLevel.A;
        if (total >= 60) return RecommendedLevel.B;
        return RecommendedLevel.C;
    }

    private static void Add(ScoreResult r, string group, string item, int points, int max)
        => r.Lines.Add(new ScoreLine(group, item, points, max));
}
