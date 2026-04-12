using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VGR.Application.Vårdval;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical;
using VGR.Technical.Testing;

namespace VGR.Application.Stories;

public class SkapaVårdvalStories
{
    [Fact]
    public async Task LyckadSkapning_GerUtfallOkMedVårdvalId()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h).MedRegion("14").MedPerson("19900101-1234").Bygg();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, s.Clock);
        var result = await interactor.ProcessAsync(
            new SkapaVårdvalCmd(s.Person!.Id, "HSA-ENHET-1", new DateOnly(2024, 1, 1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(default(VårdvalId));
    }

    [Fact]
    public async Task LyckadSkapning_TillsvidareVårdval()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h).MedRegion("14").MedPerson("19900101-1234").Bygg();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, s.Clock);
        await interactor.ProcessAsync(
            new SkapaVårdvalCmd(s.Person!.Id, "HSA-ENHET-1", new DateOnly(2024, 1, 1), null),
            CancellationToken.None);

        var vårdval = await h.Read.Vårdval
            .FirstAsync(v => v.PersonId == s.Person.Id);

        vårdval.Period.Slut.Should().BeNull("tillsvidare-vårdval har inget slut");
        vårdval.ÄrAktivt.Should().BeTrue();
    }

    [Fact]
    public async Task NyttVårdval_AvslustarBefintligtAktivt()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h)
            .MedRegion("14")
            .MedPerson("19900101-1234")
            .MedVårdval("HSA-ENHET-1", new DateOnly(2024, 1, 1))
            .Bygg();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, s.Clock);
        var result = await interactor.ProcessAsync(
            new SkapaVårdvalCmd(s.Person!.Id, "HSA-ENHET-2", new DateOnly(2024, 6, 1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var alla = await h.Read.Vårdval
            .Where(v => v.PersonId == s.Person.Id)
            .OrderBy(v => v.Period.Start)
            .ToListAsync();

        alla.Should().HaveCount(2);
        alla[0].Period.Slut.Should().NotBeNull("gamla vårdvalet ska ha avslutats");
        alla[1].Period.Slut.Should().BeNull("nya vårdvalet ska vara tillsvidare");
    }

    [Fact]
    public async Task NyttVårdval_UtanBefintligtAktivt_SkaparDirekt()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h)
            .MedRegion("14")
            .MedPerson("19900101-1234")
            .MedVårdval("HSA-ENHET-1", new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1))
            .Bygg();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, s.Clock);
        var result = await interactor.ProcessAsync(
            new SkapaVårdvalCmd(s.Person!.Id, "HSA-ENHET-2", new DateOnly(2024, 7, 1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Överlapp_SammaEnhet_KastarInvariantViolation()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h)
            .MedRegion("14")
            .MedPerson("19900101-1234")
            .MedVårdval("HSA-ENHET-1", new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31))
            .Bygg();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, s.Clock);
        var act = () => interactor.ProcessAsync(
            new SkapaVårdvalCmd(s.Person!.Id, "HSA-ENHET-1", new DateOnly(2024, 6, 1), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainInvariantViolationException>();
    }

    [Fact]
    public async Task PersonSaknas_KastarDomainException()
    {
        await using var h = new SqliteHarness();
        var clock = new TestClock();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, clock);
        var act = () => interactor.ProcessAsync(
            new SkapaVårdvalCmd(new PersonId(Guid.NewGuid()), "HSA-ENHET-1", new DateOnly(2024, 1, 1), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainAggregateNotFoundException>();
    }

    [Fact]
    public async Task OgiltigtHsaId_KastarDomainException()
    {
        await using var h = new SqliteHarness();
        var s = await new TestScenario(h).MedRegion("14").MedPerson("19900101-1234").Bygg();

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, s.Clock);
        var act = () => interactor.ProcessAsync(
            new SkapaVårdvalCmd(s.Person!.Id, "", new DateOnly(2024, 1, 1), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
