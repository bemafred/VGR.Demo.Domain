
using System;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using Xunit;
using FluentAssertions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.Tests;

public class PersonTests
{
    [Fact]
    public void SkapaPerson_SätterFält()
    {
        var pnr = Personnummer.Parse("19900101-1234");
        var nu = new DateTimeOffset(2024,1,1,12,0,0,TimeSpan.Zero);

        var region = Region.Skapa("14");
        var p = region.SkapaPerson(pnr, nu);

        p.Personnummer.Should().Be(pnr);
        p.SkapadTid.Should().Be(nu);
        p.AllaVårdval.Should().BeEmpty();
    }

    [Fact]
    public void SkapaVårdval_Överlapp_Kastar()
    {
        var region = Region.Skapa("14");
        var p = region.SkapaPerson(Personnummer.Parse("19900101-1234"), DateTimeOffset.UtcNow);
        var enhet = HsaId.Tolka("HSA-ENHET-1");
        var läkare = HsaId.Tolka("HSA-LAKARE-1");

        p.SkapaVårdval(enhet, Tidsrymd.Skapa(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30)), DateTimeOffset.UtcNow);

        Action act = () => p.SkapaVårdval(enhet, Tidsrymd.SkapaTillsvidare(new DateOnly(2024, 6, 1)), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainInvariantViolationException>()
           .WithMessage("*Överlappande vårdval*");
    }
}
