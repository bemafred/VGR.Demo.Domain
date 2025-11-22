
using System;
using System.Linq;
using FluentAssertions;
using VGR.Domain.SharedKernel;
using Xunit;

public class TidsrymdTests
{
    [Fact]
    public void Skapa_Validerar_Ordning()
    {
        var a = DateTimeOffset.Parse("2025-10-24T08:00:00+02:00");
        var b = DateTimeOffset.Parse("2025-10-24T10:00:00+02:00");
        var t = Tidsrymd.Skapa(a, b);
        t.Varaktighet.Should().Be(TimeSpan.FromHours(2));
        t.ÄrTomt.Should().BeFalse();
    }

    [Fact]
    public void Snitt_och_Sammanslagning_Fungerar()
    {
        var a = Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T08:00:00+02:00"),
                                    DateTimeOffset.Parse("2025-10-24T10:00:00+02:00"));
        var b = Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T09:00:00+02:00"),
                                    DateTimeOffset.Parse("2025-10-24T11:00:00+02:00"));

        var snitt = a.Snitt(b);
        snitt.Should().NotBeNull();
        snitt!.Value.Varaktighet.Should().Be(TimeSpan.FromHours(1));

        var sam = a.Sammanfoga(b);
        sam.Should().NotBeNull();
        sam!.Value.Varaktighet.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public void Normalisera_SlårIhop_Angränsande()
    {
        var a = Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T08:00:00+02:00"),
                                    DateTimeOffset.Parse("2025-10-24T09:00:00+02:00"));
        var b = Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T09:00:00+02:00"),
                                    DateTimeOffset.Parse("2025-10-24T10:00:00+02:00"));
        var c = Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T11:00:00+02:00"),
                                    DateTimeOffset.Parse("2025-10-24T12:00:00+02:00"));

        var norm = Tidsrymd.Normalisera([b, c, a]).ToArray();
        norm.Should().HaveCount(2);
        norm[0].Varaktighet.Should().Be(TimeSpan.FromHours(2));
        norm[1].Varaktighet.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void FriaLuckor_Respekterar_Fönster()
    {
        var fönster = Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T08:00:00+02:00"),
                                          DateTimeOffset.Parse("2025-10-24T12:00:00+02:00"));
        var upptagna = new[] {
            Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T08:30:00+02:00"),
                                DateTimeOffset.Parse("2025-10-24T09:00:00+02:00")),

            Tidsrymd.Skapa(DateTimeOffset.Parse("2025-10-24T10:00:00+02:00"),
                                DateTimeOffset.Parse("2025-10-24T11:00:00+02:00"))
        };

        var luckor = TidsrymdAlgoritmer.FriaLuckor(fönster, upptagna).ToArray();
        luckor.Should().ContainSingle(l => l.Varaktighet == TimeSpan.FromMinutes(30));
        luckor.Count(l => l.Varaktighet == TimeSpan.FromMinutes(60)).Should().Be(2);
        luckor.Should().HaveCount(3);
    }

    [Fact]
    public void Datumintervall_Till_Tidsrymd_Stockholm_DST()
    {
        // Övergång normaltid -> sommartid 2025-03-30 i Europe/Stockholm (02:00 -> 03:00).
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var d = Datumintervall.Skapa(new DateOnly(2025, 3, 30), new DateOnly(2025, 3, 31));
        var t = d.TillTidsrymd(tz);

        // Varaktigheten kan bli 23 timmar pga borttappad timme; vi validerar halvöppenhet och ordning.
        (t.Slut > t.Start).Should().BeTrue();
        t.Innehåller(t.Start).Should().BeTrue();
        t.Innehåller(t.Slut).Should().BeFalse();
    }

    [Fact]
    public void Konverteringar_Rundresa_Behåller_Dagar()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var d = Datumintervall.Skapa(new DateOnly(2025, 10, 24), new DateOnly(2025, 10, 27));
        var t = d.TillTidsrymd(tz);
        var tillbaka = t.TillDatumintervall();
        tillbaka.Should().Be(d);
    }
}
