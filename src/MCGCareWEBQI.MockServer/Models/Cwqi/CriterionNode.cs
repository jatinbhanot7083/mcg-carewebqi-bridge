namespace MCGCareWEBQI.MockServer.Models.Cwqi;

public enum CriterionGate { None, AnyOf, AllOf }
public enum NodeStatus    { Unset, Met, NotMet }

/// One node in the Clinical Indications tree (parent OR leaf).
/// Parents have a Gate ("1 or more of" / "ALL of") and Children;
/// leaves have a Checked flag. Status of a parent can be derived
/// from children or explicitly overridden.
public sealed class CriterionNode
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    public string Text { get; init; } = "";

    /// "1 or more of the following" / "ALL of the following" / none.
    public CriterionGate Gate { get; init; } = CriterionGate.None;

    /// Optional badge numbers in parens, e.g. "(1) (2) (3)".
    public int[] Counts { get; init; } = Array.Empty<int>();

    /// Optional reference letters like [A], [B] (each is an index into Guideline.References).
    public string[] References { get; init; } = Array.Empty<string>();

    /// True for leaves with a checkbox.
    public bool IsLeaf => Children.Count == 0;

    /// Set for leaves when the clinician checks the box.
    public bool Checked { get; set; }

    /// Set for parents (explicit override). If Unset, derive from children.
    public NodeStatus? Override { get; set; }

    /// Per-criterion free-text note.
    public string Note { get; set; } = "";

    /// True for criteria that require a justification when checked (e.g. Medicare 2-midnight exception).
    public bool RequiresJustification { get; init; }
    public int  JustificationMaxChars { get; init; } = 250;
    public string Justification        { get; set; } = "";

    /// Inline calculator references (purely decorative for the demo).
    public CalculatorRef[] Calculators { get; init; } = Array.Empty<CalculatorRef>();

    /// Hyperlinked clinical terms inside the criterion text (shown in blue + ⓘ).
    public string[] ClinicalTerms { get; init; } = Array.Empty<string>();

    public List<CriterionNode> Children { get; init; } = new();

    /// Computed parent status. Returns the override if set, otherwise derives from children.
    public NodeStatus DerivedStatus
    {
        get
        {
            if (Override is not null) return Override.Value;
            if (IsLeaf) return Checked ? NodeStatus.Met : NodeStatus.Unset;

            return Gate switch
            {
                CriterionGate.AllOf => Children.All(c => c.DerivedStatus == NodeStatus.Met)
                                       ? NodeStatus.Met
                                       : (Children.Any(c => c.DerivedStatus == NodeStatus.NotMet)
                                            ? NodeStatus.NotMet
                                            : NodeStatus.Unset),
                CriterionGate.AnyOf => Children.Any(c => c.DerivedStatus == NodeStatus.Met)
                                       ? NodeStatus.Met
                                       : NodeStatus.Unset,
                _                   => NodeStatus.Unset,
            };
        }
    }
}

public sealed record CalculatorRef(string Label, string Tooltip);
