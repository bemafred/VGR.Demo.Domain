using Xunit;

namespace VGR.Technical.Testing;

public sealed class KräverPostgresTheoryAttribute : TheoryAttribute
{
    public override string? Skip => PostgresHarness.ÄrTillgänglig()
        ? null : "PostgreSQL-server ej tillgänglig";
}
