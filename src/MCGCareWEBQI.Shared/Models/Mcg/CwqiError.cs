using System.Xml.Linq;

namespace MCGCareWEBQI.Shared.Models.Mcg;

/// Structured view of MCG's error envelope (Dev Guide §7.1).
public sealed class CwqiError
{
    public string? Type { get; set; }
    public string? SourceIp { get; set; }
    public string? RequestId { get; set; }
    public List<CwqiErrorMessage> Messages { get; set; } = [];
    public string RawXml { get; set; } = "";

    public static bool TryParse(string xml, out CwqiError? error)
    {
        error = null;
        try
        {
            var doc = XDocument.Parse(xml);
            if (doc.Root is null || !doc.Root.Name.LocalName.Equals("cwqierror", StringComparison.OrdinalIgnoreCase))
                return false;

            error = new CwqiError
            {
                RawXml    = xml,
                Type      = doc.Root.Attribute("type")?.Value,
                SourceIp  = doc.Root.Attribute("sourceip")?.Value,
                RequestId = doc.Root.Attribute("requestid")?.Value,
                Messages  = doc.Root.Element("messages")?.Elements("message")
                    .Select(m => new CwqiErrorMessage
                    {
                        Code = m.Attribute("code")?.Value,
                        Text = (m.Value ?? "").Trim()
                    }).ToList() ?? []
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class CwqiErrorMessage
{
    public string? Code { get; set; }
    public string? Text { get; set; }
}
