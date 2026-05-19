using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// Library of seeded guidelines used by the mock to back the certification dry-run.
/// Each guideline maps roughly to a real MCG guideline code; the criteria text is
/// transcribed/paraphrased only enough to walk the cert script — not pixel-perfect.
public static class GuidelineSeeds
{
    /// All search-results metadata in one place. Cert section 2 requires MCR first,
    /// then customized, then MCG — the search page sorts by Product priority.
    public static GuidelineSearchRow[] AllSearchRows() => new[]
    {
        // ---- MCR (Medicare Compliance) — must appear FIRST per cert section 2 ----
        new GuidelineSearchRow { Code = "MCR-031",  Product = "MCR", Type = "ORG",   Title = "Medicare Nationally Covered and Noncovered Conditions",      Glos = "",       HasBenchmarkStats = false },
        new GuidelineSearchRow { Code = "MCR-018",  Product = "MCR", Type = "ORG",   Title = "Medicare Two-Midnight Rule",                                 Glos = "",       HasBenchmarkStats = false },

        // ---- ISC (Inpatient & Surgical Care) ----
        new GuidelineSearchRow { Code = "M-130",     Product = "ISC", Type = "ORG",   Title = "Diabetes",                                                 Glos = "2 (DS)", HasBenchmarkStats = true },
        new GuidelineSearchRow { Code = "M-282",     Product = "ISC", Type = "ORG",   Title = "Migraine, Status Migrainosus",                             Glos = "2 (DS)", HasBenchmarkStats = true },
        new GuidelineSearchRow { Code = "S-535",     Product = "ISC", Type = "ORG",   Title = "Heart Transplant",                                         Glos = "8 (DS)", HasBenchmarkStats = true },
        new GuidelineSearchRow { Code = "M-185",     Product = "ISC", Type = "ORG",   Title = "Headache, Non-Migraine",                                   Glos = "2 (DS)", HasBenchmarkStats = true },

        // ---- RRG (Rapid Review Guidelines) ----
        new GuidelineSearchRow { Code = "M-190RRG",  Product = "ISC", Type = "RRG",   Title = "Stroke RRG",                                               Glos = "3 (DS)" },
        new GuidelineSearchRow { Code = "M-40RRG",   Product = "ISC", Type = "RRG",   Title = "Heart Failure RRG",                                        Glos = "4 (DS)" },

        // ---- GRG (General Recovery Care) ----
        new GuidelineSearchRow { Code = "PG-ONC",    Product = "GRG", Type = "ORG",   Title = "Oncology, General Recovery",                               Glos = "" },
        new GuidelineSearchRow { Code = "PG-MTR",    Product = "GRG", Type = "ORG",   Title = "Maternity, General Recovery",                              Glos = "" },

        // ---- BHG (Behavioral Health) ----
        new GuidelineSearchRow { Code = "BH-220",    Product = "BHG", Type = "ORG",   Title = "Major Depressive Disorder",                                Glos = "" },

        // ---- AC (Ambulatory Care) ----
        new GuidelineSearchRow { Code = "A-0010",    Product = "AC",  Type = "ORG",   Title = "Hypertension — Ambulatory Management",                     Glos = "" },
    };

    /// Sort key: MCR first (1), Custom (2), everything else (3). Cert §2 requirement.
    public static int ProductSortKey(string product) => product switch
    {
        "MCR"    => 1,
        "Custom" => 2,
        _        => 3,
    };

    /// Map a diagnosis code to the guidelines that should be most relevant.
    /// Cert §1 uses G44.001 (migraine) → M-282 must surface; §3 uses M-282 explicitly.
    public static GuidelineSearchRow[] SearchByCode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return AllSearchRows().OrderBy(r => ProductSortKey(r.Product)).ToArray();
        var c = code.Trim().ToUpperInvariant();
        var all = AllSearchRows();
        // Code-driven relevance for the cert's example codes.
        var hits = c switch
        {
            "G44.001"             => all.Where(r => r.Code is "M-282" or "M-185" or "MCR-031" or "MCR-018").ToArray(),
            "E08.44" or "E10.10" or "E11.65" => all.Where(r => r.Code.Contains("130") || r.Product == "MCR").ToArray(),
            _                     => all,
        };
        return hits.OrderBy(r => ProductSortKey(r.Product)).ThenBy(r => r.Code).ToArray();
    }

    /// Look up the full guideline (with criteria tree) by code. Returns Diabetes M-130
    /// as a sensible default if the code isn't one of the explicitly-built ones.
    public static Guideline LoadGuideline(string code) => code?.ToUpperInvariant() switch
    {
        "M-130"     => DiabetesM130Seed.Build(),
        "M-282"     => MigraineM282Seed.Build(),
        "S-535"     => HeartTransplantS535Seed.Build(),
        "M-190RRG"  => StrokeM190RrgSeed.Build(),
        "M-40RRG"   => HeartFailureM40RrgSeed.Build(),
        "PG-ONC"    => OncologyPgOncSeed.Build(),
        "PG-MTR"    => MaternityPgMtrSeed.Build(),
        "MCR-031"   => MedicareCoveredMcr031Seed.Build(),
        "MCR-018"   => MedicareTwoMidnightSeed.Build(),
        _           => DiabetesM130Seed.Build(),
    };
}
