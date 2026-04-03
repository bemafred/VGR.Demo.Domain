
using System;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using Xunit;
using FluentAssertions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.Verifications;

public class VårdvalTests
{
    [Fact]
    public void Avsluta_Med_SlutFöreStart_Kastar()
    {
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        var v = person.SkapaVårdval(HsaId.Tolka("HSA-ENHET-1"), Tidsrymd.SkapaTillsvidare(new DateOnly(2024,1,1)), nu);

        var act = () => v.Avsluta(new DateOnly(2023,12,31));

        act.Should().Throw<DomainInvariantViolationException>()
           .WithMessage("*Slut * före start *");
    }
}
