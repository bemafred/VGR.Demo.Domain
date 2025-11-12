namespace VGR.Semantics.Queries.Tests;

public sealed record SamplePeriod(DateTimeOffset Start, DateTimeOffset? End)
{
    public bool Contains(DateTimeOffset t) => Start <= t && (End is null || t < End);
}

