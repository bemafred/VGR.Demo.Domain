using VGR.Domain.SharedKernel;

namespace VGR.Technical.Testing;

/// <summary>
/// Delade testscenarier för Tidsrymd-korrelationer.
/// Används av SQLite-, PostgreSQL- och SqlServer-korrelationstester via MemberData.
/// </summary>
public static class TidsrymdTestScenarier
{
    public static IEnumerable<object[]> InnehållerScenarier()
    {
        var reftid = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

        yield return [
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            reftid, true, "NULL-hantering (tillsvidare)"
        ];
        yield return [
            Tidsrymd.Skapa(reftid, reftid.AddDays(30)),
            reftid, true, "Start inkluderad (<=)"
        ];
        yield return [
            Tidsrymd.Skapa(reftid.AddDays(-30), reftid),
            reftid, false, "Slut exkluderad (<)"
        ];
        yield return [
            Tidsrymd.Skapa(reftid.AddDays(-30), reftid.AddDays(30)),
            reftid, true, "AND-operator"
        ];
    }

    public static IEnumerable<object[]> ÖverlapparScenarier()
    {
        var p1Start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var p1End = new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.Zero);
        var periode1 = Tidsrymd.Skapa(p1Start, p1End);

        yield return [
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Helt skilda intervall"
        ];
        yield return [
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 4, 30, 0, 0, 0, TimeSpan.Zero)),
            true, "Helt överlappande"
        ];
        yield return [
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 7, 31, 0, 0, 0, TimeSpan.Zero)),
            true, "Delvis överlappande"
        ];
        yield return [
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Angränsande – inte överlappande"
        ];
        yield return [
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            periode1,
            true, "Tillsvidare möter begränsad"
        ];
        yield return [
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)),
            true, "Båda tillsvidare"
        ];
    }

    public static IEnumerable<object[]> ÄrTillsvidareScenarier()
    {
        yield return [
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            true, "Slut är null"
        ];
        yield return [
            Tidsrymd.Skapa(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Slut är satt"
        ];
    }

    public static IEnumerable<object[]> VårdvalÄrAktivtScenarier()
    {
        yield return [
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            true, "Vårdval är aktivt (tillsvidare)"
        ];
        yield return [
            Tidsrymd.Skapa(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Vårdval är avslutat"
        ];
    }
}
