namespace MCGCareWEBQI.MockServer.Models.Cwqi;

public sealed class Guideline
{
    public string Code  { get; init; } = "";       // e.g. M-130
    public string Title { get; init; } = "";       // e.g. Diabetes
    public string Product { get; init; } = "";     // ISC, AC, HC, etc.
    public string Type    { get; init; } = "";     // ORG, ORG-P, RRG
    public string Glos    { get; init; } = "";     // e.g. 2 (DS)
    public string Edition { get; init; } = "28.0";
    public string[] Codes { get; init; } = Array.Empty<string>();
    public string[] References { get; init; } = Array.Empty<string>();    // [A], [B], [C]…
    public List<CriterionNode> RootSections { get; init; } = new();
}

public sealed class GuidelineSearchRow
{
    public string Code { get; init; } = "";
    public string Product { get; init; } = "";
    public string Type    { get; init; } = "";
    public string Title   { get; init; } = "";
    public string Glos    { get; init; } = "";
    public string Edition { get; init; } = "28.0";
    public bool HasBenchmarkStats { get; init; }
}
