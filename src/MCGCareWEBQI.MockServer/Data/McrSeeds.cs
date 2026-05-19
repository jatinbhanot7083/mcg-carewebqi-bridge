using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// Medicare Compliance (MCR) guidelines. Cert section 2 requires these to appear
/// FIRST in any search and tests documenting both covered + non-covered indications.

public static class MedicareCoveredMcr031Seed
{
    public static Guideline Build() => new()
    {
        Code    = "MCR-031",
        Title   = "Medicare Nationally Covered and Noncovered Conditions",
        Product = "MCR",
        Type    = "ORG",
        Edition = "28.0",
        Codes   = new[] { "Z51.11", "G44.001" },
        References = new[] { "[CMS] CMS National Coverage Determinations (NCD) Manual." },
        RootSections = new()
        {
            new()
            {
                Id = "covered", Text = "Covered indications", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "mcr031-c1", Text = "Service is medically reasonable and necessary per CMS NCD." },
                    new() { Id = "mcr031-c2", Text = "Documentation of failure of less invasive treatments first." },
                    new() { Id = "mcr031-c3", Text = "Provider has appropriate credentialing for the service." },
                }
            },
            new()
            {
                Id = "noncovered", Text = "Non-covered indications", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "mcr031-n1", Text = "Investigational or experimental treatment." },
                    new() { Id = "mcr031-n2", Text = "Service performed for cosmetic purposes only." },
                    new() { Id = "mcr031-n3", Text = "Service not within accepted standards of medical practice." },
                }
            }
        }
    };
}

public static class MedicareTwoMidnightSeed
{
    public static Guideline Build() => new()
    {
        Code    = "MCR-018",
        Title   = "Medicare Two-Midnight Rule",
        Product = "MCR",
        Type    = "ORG",
        Edition = "28.0",
        Codes   = new[] { "G44.001", "E11.65" },
        References = new[] { "[CMS] CMS Two-Midnight benchmark, 42 CFR §412.3." },
        RootSections = new()
        {
            new()
            {
                Id = "tm-rule", Text = "Inpatient admission satisfies Two-Midnight benchmark for", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() {
                        Id = "tm-cbc-exception",
                        Text = "Case-by-case exception: admitting clinician expects <2 midnights but complex medical factors justify inpatient care.",
                        RequiresJustification = true,
                        JustificationMaxChars = 250,
                    },
                    new() { Id = "tm-2-night",   Text = "Patient has already received medically necessary care meeting the 2-midnight benchmark." },
                    new() { Id = "tm-inpt-only", Text = "Procedure is on CMS Inpatient Only List." },
                }
            }
        }
    };
}
