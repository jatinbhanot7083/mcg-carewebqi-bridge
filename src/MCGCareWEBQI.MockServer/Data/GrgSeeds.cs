using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// General Recovery Care guidelines used by cert section 6.

public static class OncologyPgOncSeed
{
    public static Guideline Build() => new()
    {
        Code    = "PG-ONC",
        Title   = "Oncology, General Recovery",
        Product = "GRG",
        Type    = "ORG",
        Edition = "28.0",
        Codes   = new[] { "Z51.11" },
        RootSections = new()
        {
            new()
            {
                Id = "ind", Text = "Admission is indicated for", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "pgonc-chemo",  Text = "Chemotherapy administration requiring inpatient observation" },
                    new() { Id = "pgonc-pain",   Text = "Uncontrolled cancer-related pain requiring inpatient management" },
                    new() { Id = "pgonc-neutro", Text = "Neutropenic fever or pancytopenia" },
                }
            }
        }
    };
}

public static class MaternityPgMtrSeed
{
    public static Guideline Build() => new()
    {
        Code    = "PG-MTR",
        Title   = "Maternity, General Recovery",
        Product = "GRG",
        Type    = "ORG",
        Edition = "28.0",
        Codes   = new[] { "O80", "O82" },
        RootSections = new()
        {
            new()
            {
                Id = "ind", Text = "Admission is indicated for", Gate = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "pgmtr-delivery", Text = "Vaginal delivery — routine recovery" },
                    new() { Id = "pgmtr-csec",     Text = "Cesarean section — surgical recovery" },
                    new() { Id = "pgmtr-postp",    Text = "Postpartum complication requiring inpatient care" },
                }
            }
        }
    };
}
