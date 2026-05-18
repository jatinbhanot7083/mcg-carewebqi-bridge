using MCGCareWEBQI.Shared.Configuration;
using MCGCareWEBQI.Shared.Hashing;
using MCGCareWEBQI.Shared.Models.Launch;

namespace MCGCareWEBQI.Shared.Models.Mcg;

/// Builds the form-post field list (Dev Guide §4) that gets submitted to interfacelogin.aspx.
/// Output is an ordered, signed list of name/value pairs ready to render as an HTML form
/// or to send as application/x-www-form-urlencoded.
public static class McgRequestBuilder
{
    /// Build the field dictionary for a launch, then compute messageHash and append it.
    public static IReadOnlyList<KeyValuePair<string, string>> Build(
        LaunchRequest launch,
        McgOptions mcg,
        string bridgeReturnUrl,
        string? cwqiTransactionId = null)
    {
        var fields = new Dictionary<string, string?>(StringComparer.Ordinal);

        // Base API request message (§4.2)
        Add(fields, "documentingUser",          launch.DocumentingUser ?? "Api-User");
        Add(fields, "documentingUserFirstName", launch.DocumentingUserFirstName);
        Add(fields, "documentingUserLastName",  launch.DocumentingUserLastName);
        Add(fields, "documentingUserEmail",     launch.DocumentingUserEmail);
        Add(fields, "requestID",                cwqiTransactionId);
        Add(fields, "requestType",              launch.RequestType ?? mcg.DefaultRequestType);
        Add(fields, "requestVersion",           mcg.RequestVersion);
        Add(fields, "returnUrl",                bridgeReturnUrl);
        Add(fields, "isInteractive",            mcg.IsInteractive ? "True" : "False");
        Add(fields, "resultTransform",          mcg.ResultTransform);
        Add(fields, "guidelinePublicationCodes", mcg.GuidelinePublicationCodes);

        // Episode (§4.3)
        Add(fields, "episodeID",                  launch.EpisodeId);
        Add(fields, "episodeType",                launch.EpisodeType);
        Add(fields, "episodeAdmitDate",           launch.EpisodeAdmitDate);
        Add(fields, "episodeRequestedAdmitDate",  launch.EpisodeRequestedAdmitDate);
        Add(fields, "episodeDischargeDate",       launch.EpisodeDischargeDate);
        Add(fields, "episodeDischargeTo",         launch.EpisodeDischargeTo);
        Add(fields, "episodeDischargeNote",       launch.EpisodeDischargeNote);
        Add(fields, "episodeCodes",               launch.EpisodeCodes);

        // Patient (§4.3)
        Add(fields, "patientID",              launch.PatientId);
        Add(fields, "patientFirstName",       launch.PatientFirstName);
        Add(fields, "patientMI",              launch.PatientMI);
        Add(fields, "patientLastName",        launch.PatientLastName);
        Add(fields, "patientDateOfBirth",     launch.PatientDateOfBirth);
        Add(fields, "patientGender",          launch.PatientGender);
        Add(fields, "patientBenefitPlanName", launch.PatientBenefitPlanName);
        Add(fields, "patientAddress1",        launch.PatientAddress1);
        Add(fields, "patientCity",            launch.PatientCity);
        Add(fields, "patientState",           launch.PatientState);
        Add(fields, "patientZipCode",         launch.PatientZipCode);
        Add(fields, "patientEmail",           launch.PatientEmail);
        Add(fields, "patientHomePhone",       launch.PatientHomePhone);

        // Facility (§4.3)
        Add(fields, "facilityID",            launch.FacilityId);
        Add(fields, "facilityName",          launch.FacilityName);
        Add(fields, "facilityTaxID",         launch.FacilityTaxId);
        Add(fields, "facilityAddress1",      launch.FacilityAddress1);
        Add(fields, "facilityCity",          launch.FacilityCity);
        Add(fields, "facilityState",         launch.FacilityState);
        Add(fields, "facilityZipCode",       launch.FacilityZipCode);
        Add(fields, "facilityBusinessPhone", launch.FacilityBusinessPhone);
        Add(fields, "facilityEmail",         launch.FacilityEmail);

        // Providers (§4.3)
        Add(fields, "attendingProviderID",        launch.AttendingProviderId);
        Add(fields, "attendingProviderFirstName", launch.AttendingProviderFirstName);
        Add(fields, "attendingProviderLastName",  launch.AttendingProviderLastName);
        Add(fields, "pcpID",        launch.PcpId);
        Add(fields, "pcpFirstName", launch.PcpFirstName);
        Add(fields, "pcpLastName",  launch.PcpLastName);

        // Guideline (§4.6 / §4.7)
        Add(fields, "guidelineSearchTerms", launch.GuidelineSearchTerms);

        // Compute messageHash over everything we've added (loginKey is the salt — NOT a field).
        var algo = CwqiHash.Parse(mcg.HashAlgorithm);
        var hash = CwqiHash.ComputeForFields(fields!, mcg.LoginKey, algo);
        fields["messageHash"] = hash;

        return fields
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value!))
            .ToList();
    }

    private static void Add(Dictionary<string, string?> dict, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value)) dict[key] = value;
    }
}
