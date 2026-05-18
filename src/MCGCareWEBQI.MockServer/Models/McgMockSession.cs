using MCGCareWEBQI.MockServer.Data;
using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Models;

/// In-memory representation of one inbound MCG documentation session.
/// Holds the inbound POST fields, the selected guideline, criteria selections, and clinician notes.
public sealed class McgMockSession
{
    public Guid SessionId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// All form fields received from the bridge (after hash validation).
    public Dictionary<string, string> InboundFields { get; init; } = new(StringComparer.Ordinal);

    public string RequestType    => InboundFields.GetValueOrDefault("requestType", "documentation");
    public string ReturnUrl      => InboundFields.GetValueOrDefault("returnUrl", "");
    public string EpisodeId      => InboundFields.GetValueOrDefault("episodeID", $"EPS-{DateTime.UtcNow:yyyyMMddHHmmss}BF{Random.Shared.Next(8):X1}");
    public string RequestId      => InboundFields.GetValueOrDefault("requestID", "");
    public string DocumentingUser => InboundFields.GetValueOrDefault("documentingUser", "Api-User");

    public string? PatientId        => InboundFields.GetValueOrDefault("patientID");
    public string? PatientFirstName => InboundFields.GetValueOrDefault("patientFirstName");
    public string? PatientLastName  => InboundFields.GetValueOrDefault("patientLastName");
    public string? PatientMI        => InboundFields.GetValueOrDefault("patientMI");
    public string? PatientDob       => InboundFields.GetValueOrDefault("patientDateOfBirth");
    public string? PatientGender    => InboundFields.GetValueOrDefault("patientGender");

    public string? EpisodeAdmitDate => InboundFields.GetValueOrDefault("episodeAdmitDate");
    public string? EpisodeType      => InboundFields.GetValueOrDefault("episodeType");
    public string? EpisodeCodes     => InboundFields.GetValueOrDefault("episodeCodes");

    /// First episodeCode formatted as the primary diagnosis (cosmetic).
    public string PrimaryDiagnosis
    {
        get
        {
            var code = EpisodeCodes?.Split('$', '|').FirstOrDefault();
            if (string.IsNullOrEmpty(code)) return "(no primary code)";
            return code switch
            {
                "E08.44" => $"{code}: Diabetes mellitus due to underlying condition with diabetic neuropathy",
                "E10.10" => $"{code}: Type 1 diabetes mellitus with ketoacidosis without coma",
                "E11.65" => $"{code}: Type 2 diabetes mellitus with hyperglycemia",
                _        => $"{code}",
            };
        }
    }

    /// Selected guideline (set after the user picks one in the search results).
    public Guideline? SelectedGuideline { get; set; }

    /// Clinical notes (rich text) entered at the bottom of the documentation page.
    public string ClinicalNotes { get; set; } = "";
    public string ClinicalNotesSubject { get; set; } = "None Specified";

    public bool Exited { get; set; }

    /// True once any criterion has been checked. Drives the "Selections Made, Indications Met" banner.
    public bool AnySelectionsMade => SelectedGuideline is not null && AnyCheckedRecursive(SelectedGuideline.RootSections);

    private static bool AnyCheckedRecursive(IEnumerable<CriterionNode> nodes) =>
        nodes.Any(n => n.Checked || AnyCheckedRecursive(n.Children));
}
