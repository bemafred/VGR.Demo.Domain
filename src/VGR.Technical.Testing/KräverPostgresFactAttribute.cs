using Xunit;

namespace VGR.Technical.Testing;

public sealed class KräverPostgresFactAttribute : FactAttribute
{
    public override string? Skip => PostgresHarness.ÄrTillgänglig()
        ? null : "PostgreSQL-server ej tillgänglig";
}
