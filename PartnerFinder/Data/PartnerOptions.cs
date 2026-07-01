namespace PartnerFinder.Data;

// Static option lists used to populate dropdowns in the UI.
// Kept in one place so a non-technical maintainer can add/remove choices easily.
public static class PartnerOptions
{
    // Service categories the tool is built to track (used in Add/Edit + Keyword Generator).
    public static readonly string[] ServiceCategories = new[]
    {
        "IT System Integrator",
        "Data Center Smart Hands",
        "IMAC Service Provider",
        "Break / Fix IT Service",
        "Network / Server / Storage Support",
        "Microsoft Partner",
        "Dell Partner",
        "Cisco Partner",
        "HPE Partner",
        "IT Field Service Provider",
        "AI Server Builder",
        "GPU Workstation Builder",
        "Edge AI Solution Provider",
        "Local AI Infrastructure Provider",
        "Small AI Computing Deployment Partner"
    };
}
