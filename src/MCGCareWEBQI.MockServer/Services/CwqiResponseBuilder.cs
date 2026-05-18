using System.Xml.Linq;
using MCGCareWEBQI.MockServer.Models;
using MCGCareWEBQI.MockServer.Models.Cwqi;

namespace MCGCareWEBQI.MockServer.Services;

/// Builds a CwqiMessage XML payload that mimics what real MCG would return on Exit Episode.
/// Encodes the full criteria tree state so the bridge can persist clinician-made selections.
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
                new XAttribute("Subject", s.ClinicalNotesSubject),
                new XAttribute("Created", DateTime.UtcNow.ToString("O")),
                s.ClinicalNotes));
        }
        return notesEl;
    }

    private static XElement BuildGuidelines(McgMockSession s)
    {
        var glsEl = new XElement("Guidelines");
        if (s.SelectedGuideline is { } g)
        {
            glsEl.Add(new XElement("Guideline",
                new XAttribute("GuidelineId", g.Code),
                new XAttribute("Title",       g.Title),
                new XAttribute("Product",     g.Product),
                new XAttribute("Edition",     g.Edition),
                new XAttribute("Glos",        g.Glos),
                new XAttribute("OverallStatus", OverallStatus(g).ToString()),
                new XElement("Outlines",
                    g.RootSections.SelectMany(FlattenForOutput).Select(c => BuildOutline(c, s.DocumentingUser)))));
        }
        return glsEl;
    }

    private static IEnumerable<CriterionNode> FlattenForOutput(CriterionNode n)
    {
        // Emit only nodes that contributed to the selection (checked leaves and any parent with a derived status).
        if (n.IsLeaf)
        {
            if (n.Checked) yield return n;
            yield break;
        }
        if (n.DerivedStatus != NodeStatus.Unset || !string.IsNullOrEmpty(n.Note))
            yield return n;
        foreach (var child in n.Children)
            foreach (var c in FlattenForOutput(child))
                yield return c;
    }

    private static XElement BuildOutline(CriterionNode n, string author)
    {
        var notesEl = new XElement("Notes");
        if (!string.IsNullOrEmpty(n.Note))
        {
            notesEl.Add(new XElement("Note",
                new XAttribute("Author",  author),
                new XAttribute("Created", DateTime.UtcNow.ToString("O")),
                n.Note));
        }
        if (n.RequiresJustification && !string.IsNullOrEmpty(n.Justification))
        {
            notesEl.Add(new XElement("Note",
                new XAttribute("Author",  author),
                new XAttribute("Created", DateTime.UtcNow.ToString("O")),
                new XAttribute("Subject", "Justification"),
                n.Justification));
        }
        return new XElement("Outline",
            new XAttribute("CriterionId", n.Id),
            new XAttribute("Name",        n.Text.Length > 200 ? n.Text[..200] + "…" : n.Text),
            new XAttribute("Status",      n.IsLeaf ? (n.Checked ? "Met" : "Unset") : n.DerivedStatus.ToString()),
            notesEl);
    }

    private static NodeStatus OverallStatus(Guideline g)
    {
        // Overall = Met if any root section is Met; NotMet only if explicitly all-NotMet.
        if (g.RootSections.Any(s => s.DerivedStatus == NodeStatus.Met)) return NodeStatus.Met;
        if (g.RootSections.All(s => s.DerivedStatus == NodeStatus.NotMet)) return NodeStatus.NotMet;
        return NodeStatus.Unset;
    }
}
