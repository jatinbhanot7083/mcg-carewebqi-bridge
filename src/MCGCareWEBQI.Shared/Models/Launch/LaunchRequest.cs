namespace MCGCareWEBQI.Shared.Models.Launch;

/// Caller → Bridge contract. Everything a calling application (EvokeConnect today,
/// anything else tomorrow) sends in the launch URL. This is the public surface that
/// keeps the bridge loosely coupled — callers integrate by following this contract,
/// not by linking to any DLL.
///
/// Example launch URL:
///   https://bridge/launch?
///     callerId=evokeconnect
///     &callerTxnId=550e8400-e29b-41d4-a716-446655440000
///     &requestType=documentation
///     &patientId=P-12345
///     &patientFirstName=Jane
///     &patientLastName=Doe
///     &patientDateOfBirth=1980-01-15
///     &patientGender=Female
///     &episodeType=Inpatient
///     &episodeAdmitDate=2026-05-18
///     &episodeCodes=I10|icd10cm
///     &callbackUrl=https://evokeconnect/api/mcg/callback
public sealed class LaunchRequest
{
    /// REQUIRED. Identifies the calling system. Used for logging, routing, and as the
    /// targetOrigin allowlist key when posting results back via window.postMessage.
    public string CallerId { get; set; } = "";

    /// REQUIRED. The caller's own correlation ID. Echoed back in every result so the
    /// caller can match a result to the launch it initiated.
    public string CallerTxnId { get; set; } = "";

    /// One of: documentation, episodesummary, guideline, discharge. Defaults to McgOptions.DefaultRequestType.
    public string? RequestType { get; set; }

    /// Optional. Anything the caller wants echoed back unchanged in the result payload.
    public string? ReturnContext { get; set; }

    /// Optional. If set, the bridge will POST the IntegrationResult JSON to this URL
    /// after MCG returns. Use this as a fallback for non-browser callers; modern SPA
    /// callers should rely on window.postMessage + REST poll.
    public string? CallbackUrl { get; set; }

    // ---- MCG payload values (all optional individually; required ones depend on requestType) ----
    // Field names match Dev Guide §4 verbatim so they pass through with no remapping.

    public string? EpisodeId { get; set; }
    public string? EpisodeType { get; set; }
    public string? EpisodeAdmitDate { get; set; }
    public string? EpisodeRequestedAdmitDate { get; set; }
    public string? EpisodeDischargeDate { get; set; }
    public string? EpisodeDischargeTo { get; set; }
    public string? EpisodeDischargeNote { get; set; }
    public string? EpisodeCodes { get; set; }

    public string? PatientId { get; set; }
    public string? PatientFirstName { get; set; }
    public string? PatientMI { get; set; }
    public string? PatientLastName { get; set; }
    public string? PatientDateOfBirth { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientBenefitPlanName { get; set; }
    public string? PatientAddress1 { get; set; }
    public string? PatientCity { get; set; }
    public string? PatientState { get; set; }
    public string? PatientZipCode { get; set; }
    public string? PatientEmail { get; set; }
    public string? PatientHomePhone { get; set; }

    public string? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public string? FacilityTaxId { get; set; }
    public string? FacilityAddress1 { get; set; }
    public string? FacilityCity { get; set; }
    public string? FacilityState { get; set; }
    public string? FacilityZipCode { get; set; }
    public string? FacilityBusinessPhone { get; set; }
    public string? FacilityEmail { get; set; }

    public string? AttendingProviderId { get; set; }
    public string? AttendingProviderFirstName { get; set; }
    public string? AttendingProviderLastName { get; set; }

    public string? PcpId { get; set; }
    public string? PcpFirstName { get; set; }
    public string? PcpLastName { get; set; }

    public string? DocumentingUser { get; set; }
    public string? DocumentingUserFirstName { get; set; }
    public string? DocumentingUserLastName { get; set; }
    public string? DocumentingUserEmail { get; set; }

    public string? GuidelineSearchTerms { get; set; }
}
