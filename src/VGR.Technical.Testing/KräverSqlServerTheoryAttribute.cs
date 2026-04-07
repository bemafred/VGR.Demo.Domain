using Xunit;

namespace VGR.Technical.Testing;

public sealed class KräverSqlServerTheoryAttribute : TheoryAttribute
{
    public override string? Skip => SqlServerHarness.ÄrTillgänglig()
        ? null : "SQL Server ej tillgänglig";
}
