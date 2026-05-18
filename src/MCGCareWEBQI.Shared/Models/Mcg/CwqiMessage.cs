using System.Xml.Linq;

namespace MCGCareWEBQI.Shared.Models.Mcg;

/// Structured view of MCG's response XML (Dev Guide §5). Built from a CwqiMessage
/// XDocument so we can pass typed data to callers instead of raw XML.
public sealed class CwqiMessage
{
    public string? EpisodeId { get; set; }
    public string? PatientId { get; set; }
    public string? RequestId { get; set; }
    public Patient? Patient { get; set; }
    public List<EpisodeNote> EpisodeNotes { get; set; } = [];
    public List<Guideline> Guidelines { get; set; } = [];
    /// Raw XML kept around for audit / archive purposes.
    public string RawXml { get; set; } = "";

    public static CwqiMessage Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ep = doc.Root?.Element("Episode");
        var patientEl = ep?.Element("Patient");

        var msg = new CwqiMessage
        {
            RawXml    = xml,
            EpisodeId = ep?.Attribute("EpisodeId")?.Value,
            RequestId = doc.Root?.Attribute("requestID")?.Value,
            Patient   = patientEl is null ? null : Patient.From(patientEl),
        };

        var notes = ep?.Element("EpisodeNotes")?.Elements("Note") ?? [];
        msg.EpisodeNotes.AddRange(notes.Select(EpisodeNote.From));

        var gls = ep?.Element("Guidelines")?.Elements("Guideline") ?? [];
        msg.Guidelines.AddRange(gls.Select(Guideline.From));

        msg.PatientId = msg.Patient?.PatientId;
        return msg;
    }
}

public sealed class Patient
{
    public string? PatientId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender    { get; set; }

    public static Patient From(XElement el) => new()
    {
        PatientId   = el.Attribute("PatientId")?.Value ?? el.Attribute("PatientID")?.Value,
        FirstName   = el.Attribute("FirstName")?.Value,
        LastName    = el.Attribute("LastName")?.Value,
        DateOfBirth = el.Attribute("DateOfBirth")?.Value,
        Gender      = el.Attribute("Gender")?.Value,
    };
}

public sealed class EpisodeNote
{
    public string? Author { get; set; }
    public string? Created { get; set; }
    public string? Text { get; set; }

    public static EpisodeNote From(XElement el) => new()
    {
        Author  = el.Attribute("Author")?.Value,
        Created = el.Attribute("Created")?.Value,
        Text    = el.Value,
    };
}

public sealed class Guideline
{
    public string? GuidelineId { get; set; }
    public string? Title { get; set; }
    public List<Outline> Outlines { get; set; } = [];

    public static Guideline From(XElement el) => new()
    {
        GuidelineId = el.Attribute("GuidelineId")?.Value,
        Title       = el.Attribute("Title")?.Value,
        Outlines    = el.Element("Outlines")?.Elements("Outline").Select(Outline.From).ToList() ?? [],
    };
}

public sealed class Outline
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public List<EpisodeNote> Notes { get; set; } = [];

    public static Outline From(XElement el) => new()
    {
        Name   = el.Attribute("Name")?.Value,
        Status = el.Attribute("Status")?.Value,
        Notes  = el.Element("Notes")?.Elements("Note").Select(EpisodeNote.From).ToList() ?? [],
    };
}
