
using System;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using Xunit;
using FluentAssertions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.Verifications;

public class RegionTests
{
    [Fact]
    public void SkapaPerson_Dubblett_InomRegion_Kastar()
    {
        var r = Region.Skapa("14");
        var pnr = Personnummer.Parse("19900101-1234");
        r.SkapaPerson(pnr, DateTimeOffset.UtcNow);

        Action act = () => r.SkapaPerson(pnr, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainInvariantViolationException>()
           .WithMessage("Personnummer * finns redan.");
    }
}
