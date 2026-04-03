using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Abstractions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

/// <summary>Aggregat som representerar en person inom en region.</summary>
public sealed class Person
{
    /// <summary>Unikt id för personen.</summary>
    public PersonId Id { get; private init; }
    /// <summary>Id för den region personen tillhör.</summary>
    public RegionId RegionId { get; private init; }

    /// <summary>Personens personnummer (normaliserat 12-siffrig form).</summary>
    public Personnummer Personnummer { get; private init; }
    /// <summary>Tidpunkt då personen skapades.</summary>
    public DateTimeOffset SkapadTid { get; private set; }
    /// <summary>Optimistisk samtidighetstoken.</summary>
    public byte[] RowVersion { get; private set; } = [];

    internal readonly List<Vårdval> Vårdval = [];
    /// <summary>Alla vårdval (aktiva och avslutade) för denna person.</summary>
    public IReadOnlyList<Vårdval> AllaVårdval => Vårdval;
    /// <summary>Det senaste aktiva (tillsvidare) vårdvalet, eller <c>null</c> om inget finns.</summary>
    public Vårdval? AktivtVårdval => Vårdval.LastOrDefault(v => v.ÄrAktivt);

    private readonly List<IDomainEvent> _events = [];
    private void Raise(IDomainEvent e) => _events.Add(e);
    /// <summary>Hämtar och rensar köade domänhändelser.</summary>
    public IReadOnlyList<IDomainEvent> DequeueEvents() { var c = _events.ToArray(); _events.Clear(); return c; }

    private Person() { }
    internal static Person Skapa(RegionId regionId, Personnummer pnr, DateTimeOffset nu)
        => new() { Id = PersonId.Nytt(), RegionId = regionId, Personnummer = pnr, SkapadTid = nu };

    /// <summary>Avslutar det aktiva vårdvalet per givet datum. Kastar om inget aktivt vårdval finns.</summary>
    public Vårdval AvslutaAktuelltVårdval(DateOnly slut)
    {
        var öppet = AktivtVårdval;
            
        if (öppet is null)
            Throw.Person.IngetAktivtVårdvalAttStänga();

        öppet.Avsluta(slut);
        return öppet;
    }

    /// <summary>Skapar ett nytt vårdval. Avslutar eventuellt aktivt vårdval. Kastar vid överlapp eller ogiltig ordning.</summary>
    public Vårdval SkapaVårdval(HsaId enhetHsaId, Tidsrymd giltighet, DateTimeOffset nu)
    {
        if (Vårdval.Any(v => v.EnhetsHsaId == enhetHsaId && v.Period.Överlappar(giltighet)))
            Throw.Vårdval.ÖverlappEjTillåtet(enhetHsaId, giltighet);

        var aktivt = AktivtVårdval;

        if (aktivt is not null)
        {
            if (giltighet.Start < aktivt.Period.Start)
                Throw.Vårdval.StartFöreAktivtVårdval(aktivt.Period.Start, giltighet.Start);

            AvslutaAktuelltVårdval(DateOnly.FromDateTime(giltighet.Start.DateTime));
        }

        var nytt = Domain.Vårdval.Skapa(Id, enhetHsaId, giltighet, nu);
        Vårdval.Add(nytt);

        Raise(new VårdvalSkapat(RegionId, Id, Personnummer, nu));

        return nytt;
    }

    /// <summary>Domänhändelse: ett vårdval har skapats.</summary>
    public sealed record VårdvalSkapat(RegionId RegionId, PersonId PersonId, Personnummer Personnummer, DateTimeOffset OccurredAt)
        : DomainEvent(OccurredAt);
}