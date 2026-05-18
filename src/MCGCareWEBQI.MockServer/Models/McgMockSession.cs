namespace MCGCareWEBQI.MockServer.Models;

/// In-memory representation of one inbound MCG documentation session.
/// Holds everything the clinician UI needs and everything the post-back needs.
public sealed class McgMockSession
{
    public Guid SessionId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// All form fields received from the bridge (after hash validation).
    public Dictionary<string, string> InboundFields { get; init; } = new(StringComparer.Ordinal);

    public string RequestType    => InboundFields.GetValueOrDefault("requestType", "documentation");
    public string ReturnUrl      => InboundFields.GetValueOrDefault("returnUrl", "");
    public string EpisodeId      => InboundFields.GetValueOrDefault("episodeID", $"EPS-{DateTime.UtcNow:yyyyMMddHHmmss}");
    public string RequestId      => InboundFields.GetValueOrDefault("requestID", "");
    public string DocumentingUser => InboundFields.GetValueOrDefault("documentingUser", "Api-User");

    public string? PatientId        => InboundFields.GetValueOrDefault("patientID");
    public string? PatientFirstName => InboundFields.GetValueOrDefault("patientFirstName");
    public string? PatientLastName  => InboundFields.GetValueOrDefault("patientLastName");
    public string? PatientDob       => InboundFields.GetValueOrDefault("patientDateOfBirth");
    public string? PatientGender    => InboundFields.GetValueOrDefault("patientGender");

    public string? EpisodeAdmitDate => InboundFields.GetValueOrDefault("episodeAdmitDate");
    public string? EpisodeType      => InboundFields.GetValueOrDefault("episodeType");
    public string? EpisodeCodes     => InboundFields.GetValueOrDefault("episodeCodes");

    /// Clinician-side decisions captured in the UI before Exit.
    public List<CriterionDecision> Criteria { get; init; } = SeedDefaultCriteria();
    public string ClinicalNotes { get; set; } = "";
    public bool Exited { get; set; }

    private static List<CriterionDecision> SeedDefaultCriteria() => new()
    {
        new("Inpatient admission criteria",                  CriterionStatus.Unset),
        new("Severity of illness documented",                CriterionStatus.Unset),
        new("Intensity of service appropriate for setting",  CriterionStatus.Unset),
        new("Anticipated length of stay reasonable",         CriterionStatus.Unset),
        new("Alternate level of care considered",            CriterionStatus.Unset),
        new("Discharge planning initiated",                  CriterionStatus.Unset),
    };
}

public sealed record CriterionDecision(string Description, CriterionStatus Status)
{
    public CriterionStatus Status { get; set; } = Status;
    public string Note { get; set; } = "";
}

public enum CriterionStatus { Unset, Met, NotMet, Discussed }
