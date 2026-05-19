using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// Rapid Review Guidelines used by cert section 5.

public static class StrokeM190RrgSeed
{
    public static Guideline Build() => new()
    {
        Code    = "M-190RRG",
        Title   = "Stroke RRG",
        Product = "ISC",
        Type    = "RRG",
        Glos    = "3 (DS)",
        Edition = "28.0",
        Codes   = new[] { "I63.9", "I61.9" },
        References = new[] { "[A] NIH Stroke Scale documented within 24 hours of admission." },
        RootSections = new()
        {
            new()
            {
                Id = "ind", Text = "Admission is indicated for", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "m190-acute",  Text = "Acute ischemic or hemorrhagic stroke", References = new[] { "A" } },
                    new() { Id = "m190-tia",    Text = "TIA with high ABCD2 score" },
                    new() { Id = "m190-obs",    Text = "Observation care being considered" },
                }
            }
        }
    };
}

public static class HeartFailureM40RrgSeed
{
    public static Guideline Build() => new()
    {
        Code    = "M-40RRG",
        Title   = "Heart Failure RRG",
        Product = "ISC",
        Type    = "RRG",
        Glos    = "4 (DS)",
        Edition = "28.0",
        Codes   = new[] { "I50.9", "I50.21", "I50.31" },
        References = new[] { "[A] Acute decompensated heart failure with hemodynamic instability." },
        RootSections = new()
        {
            new()
            {
                Id = "ind", Text = "Admission is indicated for", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "m40-adhf",   Text = "Acute decompensated heart failure requiring IV diuresis", References = new[] { "A" } },
                    new() { Id = "m40-hypox",  Text = "Hypoxia requiring supplemental oxygen" },
                    new() { Id = "m40-arrhyth",Text = "New onset arrhythmia requiring telemetry monitoring" },
                }
            }
        }
    };
}
