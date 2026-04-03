using FluentAssertions;
using VGR.Application.Vårdval;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical;
using VGR.Technical.Testing;

namespace VGR.Application.Stories;

public class SkapaVårdvalStories
{
    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public async Task PersonSaknas_KastarDomainException()
    {
        await using var h = new SqliteHarness();
        var clock = new TestClock();
        var ct = CancellationToken.None;

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, clock);
        var act = () => interactor.ProcessAsync(
            new SkapaVårdvalCmd(new PersonId(Guid.NewGuid()), "HSA-ENHET-1", new DateOnly(2024, 1, 1), null), ct);

        await act.Should().ThrowAsync<DomainAggregateNotFoundException>();
    }

    [Fact]
    public async Task OgiltigtHsaId_KastarDomainException()
    {
        await using var h = new SqliteHarness();
        var clock = new TestClock();
        var ct = CancellationToken.None;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), clock.UtcNow);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync(ct);

        var interactor = new SkapaVårdvalInteractor(h.Read, h.Write, clock);
        var act = () => interactor.ProcessAsync(
            new SkapaVårdvalCmd(person.Id, "", new DateOnly(2024, 1, 1), null), ct);

        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
