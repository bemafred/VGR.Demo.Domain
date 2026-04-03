using System;
using System.Linq;
using FluentAssertions;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using Xunit;

public class DatumintervallTests
{
    [Fact]
    public void Innehåller_Inkluderar_Start_Exkluderar_Slut()
    {
        var d = Datumintervall.Skapa(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10));

        d.Innehåller(new DateOnly(2025, 1, 1)).Should().BeTrue("start ingår");
        d.Innehåller(new DateOnly(2025, 1, 5)).Should().BeTrue("mitt i intervallet");
        d.Innehåller(new DateOnly(2025, 1, 9)).Should().BeTrue("sista inkluderade dagen");
        d.Innehåller(new DateOnly(2025, 1, 10)).Should().BeFalse("slut är exkluderat");
        d.Innehåller(new DateOnly(2024, 12, 31)).Should().BeFalse("före start");
    }

    [Fact]
    public void Innehåller_Tillsvidare_Inkluderar_Allt_Från_Start()
    {
        var d = Datumintervall.SkapaTillsvidare(new DateOnly(2025, 6, 1));

        d.Innehåller(new DateOnly(2025, 6, 1)).Should().BeTrue("start ingår");
        d.Innehåller(new DateOnly(2099, 12, 31)).Should().BeTrue("långt fram i tiden");
        d.Innehåller(new DateOnly(2025, 5, 31)).Should().BeFalse("före start");
    }

    [Fact]
    public void VarjeDag_Avger_Alla_Dagar_I_Halvöppet_Intervall()
    {
        var d = Datumintervall.Skapa(new DateOnly(2025, 3, 28), new DateOnly(2025, 4, 2));
        var dagar = d.VarjeDag().ToArray();

        dagar.Should().HaveCount(5);
        dagar.First().Should().Be(new DateOnly(2025, 3, 28));
        dagar.Last().Should().Be(new DateOnly(2025, 4, 1));
    }

    [Fact]
    public void VarjeDag_Tom_Intervall_Avger_Inget()
    {
        var dag = new DateOnly(2025, 6, 15);
        var d = Datumintervall.Skapa(dag, dag);

        d.ÄrTom.Should().BeTrue();
        d.VarjeDag().Should().BeEmpty();
    }

    [Fact]
    public void VarjeDag_Tillsvidare_Kastar()
    {
        var d = Datumintervall.SkapaTillsvidare(new DateOnly(2025, 1, 1));

        var act = () => d.VarjeDag().ToArray();

        act.Should().Throw<DomainUndefinedOperationException>();
    }

    [Fact]
    public void FrånTidsrymd_Bevarar_Datum()
    {
        var start = new DateOnly(2025, 4, 1);
        var slut = new DateOnly(2025, 4, 30);
        var tidsrymd = Tidsrymd.Skapa(start, slut);

        var d = Datumintervall.FrånTidsrymd(tidsrymd);

        d.Start.Should().Be(start);
        d.Slut.Should().Be(slut);
    }

    [Fact]
    public void FrånTidsrymd_Tillsvidare_Ger_Tillsvidare()
    {
        var start = new DateOnly(2025, 4, 1);
        var tidsrymd = Tidsrymd.SkapaTillsvidare(start);

        var d = Datumintervall.FrånTidsrymd(tidsrymd);

        d.Start.Should().Be(start);
        d.ÄrTillsvidare.Should().BeTrue();
    }

    [Fact]
    public void Skapa_SlutFöreStart_Kastar()
    {
        var act = () => Datumintervall.Skapa(new DateOnly(2025, 6, 15), new DateOnly(2025, 6, 10));

        act.Should().Throw<DomainValidationException>();
    }
}
