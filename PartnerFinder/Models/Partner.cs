using System.ComponentModel.DataAnnotations;

namespace PartnerFinder.Models;

// A candidate IT System Integrator / Data Center vendor / small-AI-computing builder.
// This is the single core table of the MVP. Every field below maps directly to a
// column in the SQLite database (see AppDbContext + the initial migration).
public class Partner
{
    public int Id { get; set; }

    // ---------------------------------------------------------------
    // Basic information
    // ---------------------------------------------------------------
    [Required(ErrorMessage = "Company Name is required")]
    [Display(Name = "Company Name")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(300)]
    public string? Website { get; set; }

    // Free text / dropdown, e.g. "IT System Integrator", "Edge AI Solution Provider".
    [Display(Name = "Service Category")]
    [StringLength(150)]
    public string? ServiceCategory { get; set; }

    [Display(Name = "Main Services")]
    [StringLength(1000)]
    public string? MainServices { get; set; }

    [Display(Name = "Contact Person")]
    [StringLength(150)]
    public string? ContactPerson { get; set; }

    [Display(Name = "Contact Title")]
    [StringLength(150)]
    public string? ContactTitle { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(60)]
    public string? Phone { get; set; }

    [Display(Name = "LinkedIn")]
    [StringLength(300)]
    public string? LinkedIn { get; set; }

    [Display(Name = "Source URL")]
    [StringLength(300)]
    public string? SourceUrl { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Display(Name = "Last Updated Date")]
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;

    // ---------------------------------------------------------------
    // IT / Data Center capabilities (yes / no flags)
    // ---------------------------------------------------------------
    [Display(Name = "Data Center Experience")]
    public bool DataCenterExperience { get; set; }

    [Display(Name = "Smart Hands Capability")]
    public bool SmartHandsCapability { get; set; }

    [Display(Name = "IMAC Capability")]
    public bool ImacCapability { get; set; }

    [Display(Name = "Break / Fix Capability")]
    public bool BreakFixCapability { get; set; }

    [Display(Name = "Network Support Capability")]
    public bool NetworkSupportCapability { get; set; }

    [Display(Name = "Server Support Capability")]
    public bool ServerSupportCapability { get; set; }

    [Display(Name = "Storage Support Capability")]
    public bool StorageSupportCapability { get; set; }

    [Display(Name = "Microsoft Partner Status")]
    public PartnerStatus MicrosoftPartnerStatus { get; set; } = PartnerStatus.None;

    [Display(Name = "Dell Partner Status")]
    public PartnerStatus DellPartnerStatus { get; set; } = PartnerStatus.None;

    [Display(Name = "Cisco Partner Status")]
    public PartnerStatus CiscoPartnerStatus { get; set; } = PartnerStatus.None;

    [Display(Name = "HPE Partner Status")]
    public PartnerStatus HpePartnerStatus { get; set; } = PartnerStatus.None;

    // e.g. "ISO 9001, ISO 27001"
    [StringLength(500)]
    public string? Certifications { get; set; }

    // ---------------------------------------------------------------
    // Re-Teck targeting signals: companies that lease equipment and serve
    // SMEs consume hardware continuously - the ideal downstream partner.
    // ---------------------------------------------------------------
    [Display(Name = "Equipment Leasing / HaaS")]
    public bool EquipmentLeasingSignal { get; set; }

    [Display(Name = "SME-Focused Customer Base")]
    public bool SmeFocusSignal { get; set; }

    // ---------------------------------------------------------------
    // AI Infrastructure capabilities (yes / no flags)
    // ---------------------------------------------------------------
    [Display(Name = "AI Server Build Capability")]
    public bool AiServerBuildCapability { get; set; }

    [Display(Name = "GPU Workstation Build Capability")]
    public bool GpuWorkstationBuildCapability { get; set; }

    [Display(Name = "Edge AI Deployment Capability")]
    public bool EdgeAiDeploymentCapability { get; set; }

    [Display(Name = "Local LLM Deployment Capability")]
    public bool LocalLlmDeploymentCapability { get; set; }

    [Display(Name = "NVIDIA GPU Experience")]
    public bool NvidiaGpuExperience { get; set; }

    [Display(Name = "AMD GPU Experience")]
    public bool AmdGpuExperience { get; set; }

    [Display(Name = "NVIDIA Jetson / Edge Device Experience")]
    public bool NvidiaJetsonExperience { get; set; }

    [Display(Name = "Small AI Cluster Experience")]
    public bool SmallAiClusterExperience { get; set; }

    [Display(Name = "On-Prem AI Deployment Experience")]
    public bool OnPremAiDeploymentExperience { get; set; }

    [Display(Name = "AI Model Inference Environment Setup")]
    public bool AiModelInferenceSetup { get; set; }

    [Display(Name = "Linux / Docker / Kubernetes Capability")]
    public bool LinuxDockerKubernetesCapability { get; set; }

    [Display(Name = "Cooling / Power Planning Capability")]
    public bool CoolingPowerPlanningCapability { get; set; }

    [Display(Name = "AI Infrastructure Summary")]
    [StringLength(2000)]
    public string? AiInfrastructureSummary { get; set; }

    // ---------------------------------------------------------------
    // AI / evaluation fields
    // ---------------------------------------------------------------
    [Display(Name = "AI Summary")]
    [StringLength(2000)]
    public string? AiSummary { get; set; }

    // Filled automatically by ScoringService (0-100).
    [Display(Name = "Qualification Score")]
    public int QualificationScore { get; set; }

    // Filled automatically by ScoringService (0-30, the AI portion of the total).
    [Display(Name = "AI Infrastructure Score")]
    public int AiInfrastructureScore { get; set; }

    // Filled automatically by ScoringService (A / B / C).
    [Display(Name = "Recommended Level")]
    public RecommendedLevel RecommendedLevel { get; set; } = RecommendedLevel.C;

    [Display(Name = "Manual Review Status")]
    public ManualReviewStatus ManualReviewStatus { get; set; } = ManualReviewStatus.Pending;

    [Display(Name = "Follow Up Action")]
    [StringLength(500)]
    public string? FollowUpAction { get; set; }

    // ---------------------------------------------------------------
    // Convenience helpers (not stored)
    // ---------------------------------------------------------------
    public bool HasContactInfo =>
        !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Phone);

    public bool IsAiCapable =>
        AiServerBuildCapability || GpuWorkstationBuildCapability || EdgeAiDeploymentCapability ||
        LocalLlmDeploymentCapability || NvidiaJetsonExperience || SmallAiClusterExperience ||
        OnPremAiDeploymentExperience || AiModelInferenceSetup;
}
