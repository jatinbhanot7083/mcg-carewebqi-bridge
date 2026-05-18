namespace MCGCareWEBQI.Shared.Configuration;

/// Bound to the "Mcg" configuration section. Everything in this class is
/// what changes when you swap between the stub and a real MCG tenant.
public sealed class McgOptions
{
    public const string SectionName = "Mcg";

    /// Full URL of the MCG interfacelogin endpoint.
    /// Stub: http://localhost:PORT/interface/interfacelogin.aspx
    /// Real: https://<tenant>.carewebqi.com/interface/interfacelogin.aspx
    public string InterfaceLoginUrl { get; set; } = "";

    /// Full URL of MCG's Reconcile.asmx SOAP service.
    /// Stub: http://localhost:PORT/WebServices/Reconcile.asmx
    /// Real: https://<tenant>.carewebqi.com/WebServices/Reconcile.asmx
    public string WebServicesUrl { get; set; } = "";

    /// Shared interface key (a.k.a. "Interface: Key" in CareWebQI Admin).
    /// Used as the salt for messageHash. NEVER log or echo this value.
    public string LoginKey { get; set; } = "";

    /// Hash algorithm. Dev Guide §1.2 step 3 requires SHA-256 or SHA-512 for API integrations.
    public string HashAlgorithm { get; set; } = "SHA256";

    /// API version sent as requestVersion. Dev Guide §4.2.
    public string RequestVersion { get; set; } = "12.0";

    /// Default requestType when caller doesn't specify.
    public string DefaultRequestType { get; set; } = "documentation";

    /// Default isInteractive value (Dev Guide §3.1 — True is best practice).
    public bool IsInteractive { get; set; } = true;

    /// Comma-delimited publication codes (AC, ISC, MCM, GRG, RFC, HC, CCG, BHG, TC, PIM, MCR).
    public string GuidelinePublicationCodes { get; set; } = "";

    /// XSL transform name applied to results. Per Dev Guide §5.1.
    /// Leave empty for raw XML; or EpisodeSummaryHtml.xslt, EpisodeSummaryText.xslt,
    /// EpisodeSummaryXML.xslt, EpisodeSummaryXMLWithCDATA.xslt.
    public string ResultTransform { get; set; } = "";

    /// Response delivery method: "RedirectOnly" (recommended) or "ScriptedForm". Dev Guide §3.2.
    public string InterfaceResponseType { get; set; } = "RedirectOnly";
}
