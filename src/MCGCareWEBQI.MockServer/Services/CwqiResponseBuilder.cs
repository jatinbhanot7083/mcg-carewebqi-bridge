using System.Xml.Linq;
using MCGCareWEBQI.MockServer.Models;

namespace MCGCareWEBQI.MockServer.Services;

/// Builds a CwqiMessage XML payload that mimics what real MCG would return on Exit Episode.
/// Layout follows what the legacy Receiver.aspx.cs parses (Dev Guide §5).
public static class CwqiResponseBuilder
{
    public static string Build(McgMockSession s)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("CwqiMessage",
                new XAttribute("requestID", s.RequestId),
                new XElement("Episode",
                    new XAttribute("EpisodeId", s.EpisodeId),
                    new XAttribute("RequestType", s.RequestType),
                    new XAttribute("DocumentingUser", s.DocumentingUser),
                    new XAttribute("DocumentedAt", DateTime.UtcNow.ToString("O")),
                    BuildPatient(s),
                    BuildEpisodeNotes(s),
                    BuildGuidelines(s)
                )
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildPatient(McgMockSession s) =>
        new("Patient",
            new XAttribute("PatientId",   s.PatientId ?? ""),
            new XAttribute("FirstName",   s.PatientFirstName ?? ""),
            new XAttribute("LastName",    s.PatientLastName ?? ""),
            new XAttribute("DateOfBirth", s.PatientDob ?? ""),
            new XAttribute("Gender",      s.PatientGender ?? ""));

    private static XElement BuildEpisodeNotes(McgMockSession s)
    {
        var notesEl = new XElement("EpisodeNotes");
        if (!string.IsNullOrWhiteSpace(s.ClinicalNotes))
        {
            notesEl.Add(new XElement("Note",
                new XAttribute("Author",  s.DocumentingUser),
                new XAttribute("Created", DateTime.UtcNow.ToString("O")),
                s.ClinicalNotes));
        }
        return notesEl;
    }

    private static XElement BuildGuidelines(McgMockSession s)
    {
        var outlines = new XElement("Outlines",
            s.Criteria.Select(c =>
                new XElement("Outline",
                    new XAttribute("Name",   c.Description),
                    new XAttribute("Status", c.Status.ToString()),
                    string.IsNullOrEmpty(c.Note)
                        ? null
                        : new XElement("Notes",
                            new XElement("Note",
                                new XAttribute("Author",  s.DocumentingUser),
                                new XAttribute("Created", DateTime.UtcNow.ToString("O")),
                                c.Note)))));

        return new XElement("Guidelines",
            new XElement("Guideline",
                new XAttribute("GuidelineId", "MOCK-G-001"),
                new XAttribute("Title",       "Mock General Care Guideline"),
                outlines));
    }
}
