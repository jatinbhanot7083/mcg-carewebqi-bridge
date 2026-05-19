using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// Heart Transplant (S-535). Cert sections 3 + 4 use this guideline.
public static class HeartTransplantS535Seed
{
    public static Guideline Build() => new()
    {
        Code    = "S-535",
        Title   = "Heart Transplant",
        Product = "ISC",
        Type    = "ORG",
        Glos    = "8 (DS)",
        Edition = "28.0",
        Codes   = new[] { "Z94.1", "I50.84", "I50.9" },
        References = new[]
        {
            "[A] End-stage heart failure: NYHA class IV symptoms despite optimal medical management.",
            "[B] Ventricular assist device candidacy assessed.",
            "[C] Transplant evaluation completed by multidisciplinary team.",
        },
        RootSections = new()
        {
            new()
            {
                Id    = "admission",
                Text  = "Admission is indicated for",
                Gate  = CriterionGate.AnyOf,
                Children =
                {
                    new()
                    {
                        Id    = "transplant-ind",
                        Text  = "Heart transplant indicated",
                        Gate  = CriterionGate.AllOf,
                        References = new[] { "A", "C" },
                        Children =
                        {
                            new() { Id = "s535-stage",  Text = "End-stage heart failure (NYHA class IV)", References = new[] { "A" } },
                            new() { Id = "s535-eval",   Text = "Pre-transplant evaluation complete and donor match available", References = new[] { "C" } },
                            new() { Id = "s535-vad",    Text = "VAD candidacy assessed", References = new[] { "B" } },
                        }
                    },
                    new()
                    {
                        Id    = "postop",
                        Text  = "Post-operative management following heart transplant",
                        Gate  = CriterionGate.AnyOf,
                        Children =
                        {
                            new() { Id = "s535-imm",   Text = "Immunosuppressive therapy management" },
                            new() { Id = "s535-rej",   Text = "Acute rejection requiring inpatient evaluation" },
                            new() { Id = "s535-infx", Text = "Post-transplant infection requiring inpatient management" },
                        }
                    }
                }
            }
        }
    };
}
