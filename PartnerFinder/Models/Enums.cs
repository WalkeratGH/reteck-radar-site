namespace PartnerFinder.Models;

// Partnership status with a hardware/software brand (Microsoft, Dell, Cisco, HPE).
// Anything other than "None" counts as an active brand partnership when scoring.
public enum PartnerStatus
{
    None = 0,
    Registered = 1,
    Silver = 2,
    Gold = 3,
    Certified = 4
}

// Recommended follow-up level, derived automatically from the Qualification Score.
//   A = 80-100 (contact first)
//   B = 60-79  (worth confirming)
//   C = below 60 (keep on hold)
public enum RecommendedLevel
{
    C = 0,
    B = 1,
    A = 2
}

// Where a partner sits in the human review workflow.
public enum ManualReviewStatus
{
    Pending = 0,
    InReview = 1,
    Reviewed = 2,
    NotRelevant = 3
}
