using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// Migraine, Status Migrainosus (M-282). Cert section 3 uses this guideline.
public static class MigraineM282Seed
{
    public static Guideline Build() => new()
    {
        Code    = "M-282",
        Title   = "Migraine, Status Migrainosus",
        Product = "ISC",
        Type    = "ORG",
        Glos    = "2 (DS)",
        Edition = "28.0",
        Codes   = new[] { "G44.001", "G43.001", "G43.101" },
        References = new[]
        {
            "[A] Status migrainosus is defined as a debilitating migraine attack lasting longer than 72 hours.",
            "[B] Refractory to outpatient abortive therapy attempted before admission.",
            "[C] Severe nausea/vomiting preventing oral hydration or medication.",
        },
        RootSections = new()
        {
            new()
            {
                Id    = "admission",
                Text  = "Admission is indicated for",
                Gate  = CriterionGate.AnyOf,
                Counts = new[] { 1, 2 },
                Children =
                {
                    new()
                    {
                        Id    = "status-migrainosus",
                        Text  = "Status migrainosus refractory to outpatient management",
                        Gate  = CriterionGate.AllOf,
                        Counts = new[] { 3 },
                        References = new[] { "A" },
                        Children =
                        {
                            new() { Id = "m282-duration",    Text = "Migraine attack greater than 72 hours despite outpatient therapy", References = new[] { "A" } },
                            new() { Id = "m282-refractory", Text = "Refractory to acute outpatient abortive therapy", References = new[] { "B" } },
                            new() { Id = "m282-dehydration",Text = "Severe dehydration requiring intravenous fluids", References = new[] { "C" } },
                        }
                    },
                    new()
                    {
                        Id    = "neurologic",
                        Text  = "Concerning neurologic symptom warranting inpatient evaluation",
                        Gate  = CriterionGate.AnyOf,
                        Children =
                        {
                            new() { Id = "m282-focal", Text = "New focal neurologic deficit" },
                            new() { Id = "m282-ams",   Text = "Altered mental status", ClinicalTerms = new[] { "Altered mental status" } },
                            new() { Id = "m282-szr",   Text = "Seizure activity" },
                        }
                    }
                }
            }
        }
    };
}
