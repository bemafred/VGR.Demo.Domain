
using System;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using Xunit;
using FluentAssertions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.Tests;

public class VårdvalTests
{
    [Fact]
    public void Avsluta_Med_SlutFöreStart_Kastar()
    {
        var v = Vårdval.Skapa(new VardvalId(Guid.NewGuid()), new PersonId(Guid.NewGuid()),
                              HsaId.Parse("HSA-ENHET-1"),
                              Tidsrymd.SkapaTillsvidare(new DateOnly(2024,1,1)), DateTimeOffset.UtcNow);

        var act = () => v.Avsluta(new DateOnly(2023,12,31));

        act.Should().Throw<DomainInvariantViolationException>()
           .WithMessage("*Slut * före start *");
    }
}
