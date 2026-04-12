using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Technical;

namespace VGR.Technical.Testing;

/// <summary>
/// Fluent builder för testscenarier. Skapar domänentiteter i korrekt ordning och persisterar via WriteDbContext.
/// </summary>
public sealed class TestScenario
{
    private readonly SqliteHarness _harness;
    private readonly IClock _clock;
    private string? _regionKod;
    private string? _personnummer;
    private readonly List<(string HsaId, DateOnly Start, DateOnly? Slut)> _vårdval = [];

    public TestScenario(SqliteHarness harness, IClock? clock = null)
    {
        _harness = harness;
        _clock = clock ?? new TestClock();
    }

    public TestScenario MedRegion(string kod) { _regionKod = kod; return this; }
    public TestScenario MedPerson(string personnummer) { _personnummer = personnummer; return this; }
    public TestScenario MedVårdval(string hsaId, DateOnly start, DateOnly? slut = null)
    {
        _vårdval.Add((hsaId, start, slut));
        return this;
    }

    public async Task<TestScenarioResult> Bygg(CancellationToken ct = default)
    {
        var region = Region.Skapa(_regionKod ?? "14");

        Person? person = null;
        if (_personnummer is not null)
            person = region.SkapaPerson(Personnummer.Tolka(_personnummer), _clock.UtcNow);

        Vårdval? sistaVårdval = null;
        if (person is not null)
        {
            foreach (var (hsaId, start, slut) in _vårdval)
            {
                var period = Tidsrymd.Skapa(start, slut);
                sistaVårdval = person.SkapaVårdval(HsaId.Tolka(hsaId), period, _clock.UtcNow);
            }
        }

        _harness.Write.Regioner.Add(region);
        await _harness.Write.SaveChangesAsync(ct);

        return new TestScenarioResult(region, person, sistaVårdval, _clock);
    }
}

/// <summary>Resultat från <see cref="TestScenario.Bygg"/>.</summary>
public sealed record TestScenarioResult(Region Region, Person? Person, Vårdval? Vårdval, IClock Clock);
