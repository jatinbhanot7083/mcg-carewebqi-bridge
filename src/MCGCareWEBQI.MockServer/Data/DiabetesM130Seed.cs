using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Data;

/// Diabetes M-130 Inpatient Admission criteria, transcribed from MCG Cite CareWebQI screenshots
/// for development demo purposes. Used as the canonical sample guideline behind the mock server.
public static class DiabetesM130Seed
{
    public static Guideline Build() => new()
    {
        Code    = "M-130",
        Title   = "Diabetes",
        Product = "ISC",
        Type    = "ORG",
        Glos    = "2 (DS)",
        Edition = "28.0",
        Codes   = new[] { "E08.44", "E10.10", "E10.11", "E11.00", "E11.01", "E11.65" },
        References = new[]
        {
            "[A] Risk factor reference: pregnancy, SGLT-2 use, ketogenic diet, prolonged starvation.",
            "[B] Serum beta-hydroxybutyrate 3.8 mmol/L threshold reference.",
            "[C] Arterial vs venous blood gas: pH may be 0.02–0.04 lower in arterial sampling.",
            "[D] Anion gap calculation: Na − (Cl + HCO3); normal 8–12 mEq/L.",
            "[E] Serum osmolality: 2(Na) + (Glucose/18) + (BUN/2.8). Threshold for HHS: >320 mOsm/kg.",
        },
        RootSections = new() { BuildAdmissionSection(), BuildMedicareSection() }
    };

    private static CriterionNode BuildAdmissionSection() => new()
    {
        Id    = "admission",
        Text  = "Admission is indicated for",
        Gate  = CriterionGate.AnyOf,
        Counts = new[] { 1, 2, 3 },
        Children =
        {
            new()
            {
                Id    = "dka",
                Text  = "Diabetic ketoacidosis that requires inpatient management",
                Gate  = CriterionGate.AllOf,
                Counts = new[] { 8 },
                Children =
                {
                    new()
                    {
                        Id    = "dka-glu-level",
                        Text  = "Plasma glucose level is consistent with ketoacidosis, or euglycemic diabetic ketoacidosis is suspected",
                        Gate  = CriterionGate.AnyOf,
                        Children =
                        {
                            new() { Id = "dka-hyperglu", Text = "Hyperglycemia (plasma glucose greater than 250 mg/dL (13.9 mmol/L))" },
                            new()
                            {
                                Id    = "dka-euglu",
                                Text  = "Plasma glucose is 250 mg/dL (13.9 mmol/L) or less and one or more risk factors for euglycemic diabetic ketoacidosis are present",
                                Gate  = CriterionGate.AnyOf,
                                Counts = new[] { 7, 8, 9 },
                                References = new[] { "A" },
                                Children =
                                {
                                    new() { Id = "dka-pregnancy",       Text = "Pregnancy" },
                                    new() { Id = "dka-starvation",      Text = "Prolonged starvation" },
                                    new() { Id = "dka-alcohol",         Text = "Heavy alcohol intake" },
                                    new() { Id = "dka-liverrenal",      Text = "Chronic liver or renal disease" },
                                    new() { Id = "dka-sepsis",          Text = "Sepsis or severe infection" },
                                    new() { Id = "dka-insulin-prior",   Text = "Insulin treatment prior to presentation (eg, self-treatment for observed elevated glucose)" },
                                    new() { Id = "dka-ketogenic",       Text = "Ketogenic (low carbohydrate) diet" },
                                    new() { Id = "dka-sglt2",           Text = "SGLT-2 inhibitor" },
                                    new() { Id = "dka-other",           Text = "Other risk factor for euglycemic diabetic ketoacidosis or hypoglycemia" },
                                }
                            }
                        }
                    },
                    new() { Id = "dka-ketonuria",  Text = "Ketonuria or ketonemia (eg, serum beta hydroxybutyrate 3.8 mmol/L or higher, or moderate to large ketonuria)", References = new[] { "B" } },
                    new() { Id = "dka-acidosis",   Text = "Acidosis (eg, anion gap greater than 10 (or above the upper limit of normal for laboratory), arterial or venous pH 7.30 or less, or serum bicarbonate 18 mEq/L (mmol/L) or less)", References = new[] { "C", "D" } },
                    new()
                    {
                        Id    = "dka-inpt-mgmt",
                        Text  = "Inpatient management appropriate",
                        Gate  = CriterionGate.AnyOf,
                        Counts = new[] { 2, 3 },
                        Children =
                        {
                            new() { Id = "dka-mgmt-metacid",    Text = "Metabolic acidosis (eg, anion gap greater than 10 (or above the upper limit of normal for local laboratory), or arterial or venous pH 7.30 or less) that recurs or persists despite observation care", References = new[] { "C", "D" } },
                            new() { Id = "dka-mgmt-hypoten",    Text = "Hypotension", ClinicalTerms = new[] { "Hypotension" } },
                            new() { Id = "dka-mgmt-ph",         Text = "Arterial or venous pH less than 7.0", References = new[] { "C" } },
                            new() { Id = "dka-mgmt-bicarb",     Text = "Serum bicarbonate less than 10 mEq/L (mmol/L)" },
                            new() { Id = "dka-mgmt-ams",        Text = "Altered mental status", ClinicalTerms = new[] { "Altered mental status" } },
                            new() { Id = "dka-mgmt-creat",      Text = "Rise in creatinine to 2 times its baseline value or higher (ie, reduction of more than 50% in estimated glomerular filtration rate)",
                                    Calculators = new[] { new CalculatorRef("eGFR - Adult Calculator", "Estimated GFR for adult patients"),
                                                          new CalculatorRef("eGFR - Pediatric Calculator", "Estimated GFR for pediatric patients") } },
                            new() { Id = "dka-mgmt-dehyd",      Text = "Dehydration that persists despite observation care" },
                            new() { Id = "dka-mgmt-oral",       Text = "Inability to maintain oral hydration (eg, IV fluid support required) or tolerate diet despite observation care" },
                            new() { Id = "dka-mgmt-electrolyte",Text = "Significant electrolyte abnormality (eg, hypokalemia) that persists despite observation care" },
                            new() { Id = "dka-mgmt-pregnant",   Text = "Pregnant", Counts = new[] { 14 } },
                            new() { Id = "dka-mgmt-glu-high",   Text = "Glucose level persistently too high for next level of care (eg, concern for redevelopment of dehydration, electrolyte abnormality), or not sufficiently stable despite observation care" },
                            new() { Id = "dka-mgmt-etiology",   Text = "Inpatient treatment of underlying etiology of hyperglycemia needed (eg, infection requiring inpatient care)" },
                            new() { Id = "dka-mgmt-unclear",    Text = "Etiology of diabetic ketoacidosis (DKA) unclear (ie, not thought to be secondary to missed insulin doses)" },
                            new() { Id = "dka-mgmt-noinsulin",  Text = "Patient without known outpatient insulin regimen (eg, newly diagnosed diabetes, outpatient insulin regimen not clear)" },
                        }
                    }
                }
            },
            new()
            {
                Id    = "hhs",
                Text  = "Hyperglycemic hyperosmolar state",
                Gate  = CriterionGate.AllOf,
                Counts = new[] { 1, 2, 6, 15 },
                Children =
                {
                    new() { Id = "hhs-glu",      Text = "Plasma glucose greater than 600 mg/dL (33.3 mmol/L)" },
                    new() { Id = "hhs-osmol",    Text = "Serum osmolality greater than 320 mOsm/kg (mmol/kg)", References = new[] { "E" } },
                    new() { Id = "hhs-neuro",    Text = "Neurologic dysfunction (eg, stupor, coma, seizure)" },
                    new() { Id = "hhs-ph",       Text = "Arterial or venous pH greater than 7.30", References = new[] { "C" } },
                    new() { Id = "hhs-bicarb",   Text = "Serum bicarbonate greater than 18 mEq/L (mmol/L)" },
                }
            },
            new()
            {
                Id    = "hyperglu-inpt",
                Text  = "Hyperglycemia requiring inpatient care",
                Gate  = CriterionGate.AnyOf,
                Children =
                {
                    new() { Id = "hyperglu-dehyd",     Text = "Severe dehydration requiring intravenous rehydration" },
                    new() { Id = "hyperglu-uncontrol", Text = "Uncontrolled hyperglycemia not responsive to outpatient management" },
                    new() { Id = "hyperglu-newonset",  Text = "New-onset diabetes requiring initiation of insulin therapy with educational needs" },
                }
            }
        }
    };

    private static CriterionNode BuildMedicareSection() => new()
    {
        Id    = "medicare-supp",
        Text  = "Supplemental Medicare Criteria",
        Gate  = CriterionGate.AnyOf,
        Children =
        {
            new()
            {
                Id    = "medicare-coverage",
                Text  = "Patient with Medicare coverage requires inpatient admission",
                Gate  = CriterionGate.AnyOf,
                Counts = new[] { 1 },
                Children =
                {
                    new() {
                        Id = "medicare-2midnight-exc",
                        Text = "Admitting clinician expects patient to require hospital care for less than 2 midnights but, based on complex medical factors documented in medical record, judges that inpatient care is necessary (case-by-case exception). The medical record must contain sufficient documentation to make clear the rationale for the exception.",
                        RequiresJustification = true,
                        JustificationMaxChars = 250,
                    },
                    new() { Id = "medicare-intubation",  Text = "Patient has need for intubation and mechanical ventilation that is new (ie, did not present to hospital already on mechanical ventilation)." },
                    new() { Id = "medicare-inpt-only",   Text = "Treatment plan for hospital admission includes procedure designated by CMS as inpatient only (ie, on Inpatient Only List)." },
                    new() { Id = "medicare-2midnight",   Text = "Patient has already received medically necessary hospital care that meets 2-midnight benchmark (excluding activities such as triage/intake, delays in provision of care, or time added due to patient or family convenience). The medical record must contain sufficient documentation to make clear the medical necessity for hospital care across 2 or more midnights." },
                }
            }
        }
    };

    public static GuidelineSearchRow[] DiabetesSearchResults() => new[]
    {
        new GuidelineSearchRow { Code = "M-130",     Product = "ISC", Type = "ORG",   Title = "Diabetes",                                 Glos = "2 (DS)", HasBenchmarkStats = true },
        new GuidelineSearchRow { Code = "P-140",     Product = "ISC", Type = "ORG-P", Title = "Diabetes, Pediatric",                      Glos = "2 (DS)", HasBenchmarkStats = true },
        new GuidelineSearchRow { Code = "M-130-RRG", Product = "ISC", Type = "RRG",   Title = "Diabetes RRG",                             Glos = "2 (DS)" },
        new GuidelineSearchRow { Code = "P-140-RRG", Product = "ISC", Type = "RRG-P", Title = "Diabetes, Pediatric RRG",                  Glos = "2 (DS)" },
        new GuidelineSearchRow { Code = "OC-014",    Product = "ISC", Type = "OCG",   Title = "Diabetes: Observation Care",               Glos = "" },
        new GuidelineSearchRow { Code = "R-0017",    Product = "AC",  Type = "RMG",   Title = "Diabetes Mellitus - Referral Management",  Glos = "" },
        new GuidelineSearchRow { Code = "R-0068",    Product = "AC",  Type = "RMG",   Title = "Diabetic Neuropathy - Referral Management",Glos = "" },
        new GuidelineSearchRow { Code = "M-5130",    Product = "RFC", Type = "ORG",   Title = "Diabetes",                                 Glos = "" },
        new GuidelineSearchRow { Code = "CMG-008-RF",Product = "RFC", Type = "ORG",   Title = "Heart Failure and Diabetes - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-006-RF",Product = "RFC", Type = "ORG",   Title = "Diabetes and Renal Failure - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-005-RF",Product = "RFC", Type = "ORG",   Title = "Diabetes and Hypertension - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-022-RF",Product = "RFC", Type = "ORG",   Title = "Stroke and Diabetes - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-003-RF",Product = "RFC", Type = "ORG",   Title = "Cellulitis and Diabetes - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-019-RF",Product = "RFC", Type = "ORG",   Title = "Pneumonia and Diabetes - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "M-2130",    Product = "HC",  Type = "ORG",   Title = "Diabetes",                                 Glos = "" },
        new GuidelineSearchRow { Code = "P-2140",    Product = "HC",  Type = "ORG",   Title = "Diabetes, Pediatric",                      Glos = "" },
        new GuidelineSearchRow { Code = "CMG-008-H", Product = "HC",  Type = "ORG",   Title = "Heart Failure and Diabetes - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-006-H", Product = "HC",  Type = "ORG",   Title = "Diabetes and Renal Failure - Comorbidity Management", Glos = "" },
        new GuidelineSearchRow { Code = "CMG-005-H", Product = "HC",  Type = "ORG",   Title = "Diabetes and Hypertension - Comorbidity Management", Glos = "" },
    };
}
