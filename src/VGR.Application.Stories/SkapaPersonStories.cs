using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VGR.Application.Personer;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical;
using VGR.Technical.Testing;

namespace VGR.Application.Stories;

public class SkapaPersonStories
{

    [Fact]
    public async Task LyckadSkapning_GerUtfallOkMedPersonId()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h).MedRegion("14").Bygg();

        var interactor = new SkapaPersonInteractor(h.Read, h.Write, s.Clock);
        var result = await interactor.ProcessAsync(
            new SkapaPersonCmd(s.Region.Id, "19900101-1234"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(default(PersonId));
    }

    [Fact]
    public async Task LyckadSkapning_PersisterarPerson()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h).MedRegion("14").Bygg();

        var interactor = new SkapaPersonInteractor(h.Read, h.Write, s.Clock);
        await interactor.ProcessAsync(
            new SkapaPersonCmd(s.Region.Id, "19900101-1234"), CancellationToken.None);

        var person = await h.Read.Personer.FirstAsync();
        person.Personnummer.Should().Be(Personnummer.Tolka("19900101-1234"));
        person.RegionId.Should().Be(s.Region.Id);
    }

    [Fact]
    public async Task Dubblett_InomRegion_GerUtfallFail()
    {
        await using var h = new SqliteHarness();
        var clock = new TestClock();
        var ct = CancellationToken.None;

        var region = Region.Skapa("14");
        region.SkapaPerson(Personnummer.Tolka("19900101-1234"), clock.UtcNow);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync(ct);

        var interactor = new SkapaPersonInteractor(h.Read, h.Write, clock);
        var result = await interactor.ProcessAsync(
            new SkapaPersonCmd(region.Id, "19900101-1234"), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("redan registrerat");
    }

    [Fact]
    public async Task RegionSaknas_KastarDomainException()
    {
        await using var h = new SqliteHarness();
        var clock = new TestClock();
        var ct = CancellationToken.None;

        var interactor = new SkapaPersonInteractor(h.Read, h.Write, clock);
        var act = () => interactor.ProcessAsync(
            new SkapaPersonCmd(new RegionId(Guid.NewGuid()), "19900101-1234"), ct);

        await act.Should().ThrowAsync<DomainAggregateNotFoundException>();
    }

    [Fact]
    public async Task OgiltigtPersonnummer_KastarDomainException()
    {
        await using var h = new SqliteHarness();
        var clock = new TestClock();
        var ct = CancellationToken.None;

        var region = Region.Skapa("14");
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync(ct);

        var interactor = new SkapaPersonInteractor(h.Read, h.Write, clock);
        var act = () => interactor.ProcessAsync(
            new SkapaPersonCmd(region.Id, "ogiltigt"), ct);

        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
