using PartnerFinder.Models;
using PartnerFinder.Services;

namespace PartnerFinder.Data;

// Inserts a few US-based sample partners the first time the app runs so the
// Dashboard and lists are not empty. Safe to run repeatedly - it does nothing
// once any partner exists.
public static class DbSeeder
{
    public static void Seed(AppDbContext db, ScoringService scoring)
    {
        if (db.Partners.Any()) return;

        var partners = new List<Partner>
        {
            new()
            {
                CompanyName = "Summit AI Infrastructure LLC",
                Country = "United States",
                City = "Austin, TX",
                Website = "https://summit-ai-infra.example.com",
                ServiceCategory = "AI Server Builder",
                MainServices = "GPU server builds, on-prem AI clusters, data center smart hands",
                ContactPerson = "Dana Whitmore",
                ContactTitle = "Head of Solutions",
                Email = "dana@summit-ai-infra.example.com",
                Phone = "+1 512-555-0142",
                LinkedIn = "https://linkedin.com/company/summit-ai-infra",
                SourceUrl = "https://summit-ai-infra.example.com/about",
                Notes = "Strong NVIDIA focus. Sample seed record.",
                DataCenterExperience = true,
                SmartHandsCapability = true,
                ServerSupportCapability = true,
                StorageSupportCapability = true,
                NetworkSupportCapability = true,
                DellPartnerStatus = PartnerStatus.Gold,
                Certifications = "ISO 9001, ISO 27001",
                AiServerBuildCapability = true,
                GpuWorkstationBuildCapability = true,
                EdgeAiDeploymentCapability = true,
                LocalLlmDeploymentCapability = true,
                NvidiaGpuExperience = true,
                SmallAiClusterExperience = true,
                OnPremAiDeploymentExperience = true,
                AiModelInferenceSetup = true,
                LinuxDockerKubernetesCapability = true,
                CoolingPowerPlanningCapability = true,
                AiInfrastructureSummary = "Builds small on-prem AI clusters and GPU workstations.",
                ManualReviewStatus = ManualReviewStatus.Pending,
                FollowUpAction = "Schedule intro call"
            },
            new()
            {
                CompanyName = "Beacon IT Field Services Inc",
                Country = "United States",
                City = "Columbus, OH",
                Website = "https://beacon-itfield.example.com",
                ServiceCategory = "IT Field Service Provider",
                MainServices = "IMAC, break/fix, network and server support nationwide",
                ContactPerson = "Marcus Reed",
                ContactTitle = "Operations Manager",
                Email = "marcus@beacon-itfield.example.com",
                Phone = "+1 614-555-0199",
                SourceUrl = "https://beacon-itfield.example.com",
                Notes = "Good national coverage. Sample seed record.",
                DataCenterExperience = true,
                SmartHandsCapability = true,
                ImacCapability = true,
                BreakFixCapability = true,
                NetworkSupportCapability = true,
                ServerSupportCapability = true,
                MicrosoftPartnerStatus = PartnerStatus.Silver,
                Certifications = "ISO 9001",
                ManualReviewStatus = ManualReviewStatus.Pending,
                FollowUpAction = "Confirm coverage areas"
            },
            new()
            {
                CompanyName = "Cascade Edge Computing",
                Country = "United States",
                City = "Seattle, WA",
                Website = "https://cascade-edge.example.com",
                ServiceCategory = "Edge AI Solution Provider",
                MainServices = "Edge AI deployments, NVIDIA Jetson integration",
                Notes = "No contact captured yet - needs research. Sample seed record.",
                EdgeAiDeploymentCapability = true,
                NvidiaJetsonExperience = true,
                NvidiaGpuExperience = true,
                LinuxDockerKubernetesCapability = true,
                AiModelInferenceSetup = true,
                AiInfrastructureSummary = "Edge inference specialists.",
                ManualReviewStatus = ManualReviewStatus.Pending
            }
        };

        foreach (var p in partners)
        {
            p.LastUpdatedDate = DateTime.UtcNow;
            scoring.Apply(p);
        }

        db.Partners.AddRange(partners);
        db.SaveChanges();
    }
}
