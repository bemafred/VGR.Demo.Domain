using Xunit;

namespace VGR.Technical.Testing;

public sealed class KräverSqlServerFactAttribute : FactAttribute
{
    public override string? Skip => SqlServerHarness.ÄrTillgänglig()
        ? null : "SQL Server ej tillgänglig";
}
