using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

public sealed class Person
{
    public PersonId Id { get; private init; }
    public RegionId RegionId { get; private init; }

    public Personnummer Personnummer { get; private init; }
    public DateTimeOffset SkapadTid { get; private set; }

    internal readonly List<Vårdval> Vårdval = [];
    public IReadOnlyList<Vårdval> AllaVårdval => Vårdval;
    public Vårdval? AktivtVårdval => Vårdval.LastOrDefault(v => v.ÄrÖppet); 

    private readonly List<IDomainEvent> _events = [];
    private void Raise(IDomainEvent e) => _events.Add(e);
    public IReadOnlyList<IDomainEvent> DequeueEvents() { var c = _events.ToArray(); _events.Clear(); return c; }

    private Person() { }
    internal static Person Skapa(RegionId regionId, Personnummer pnr, DateTimeOffset nu)
        => new() { Id = new(), RegionId = regionId, Personnummer = pnr, SkapadTid = nu };

    public Vårdval AvslutaAktuelltVårdval(DateOnly slut)
    {
        var öppet = AktivtVårdval;
            
        if (öppet is null)
            Throw.Vårdval.IngetAktivtVårdvalFinns();

        öppet.Avsluta(slut);
        return öppet;
    }

    public Vårdval SkapaVårdval(HsaId enhetHsaId, Tidsrymd giltighet, DateTimeOffset nu)
    {
        if (Vårdval.Any(v => v.EnhetsHsaId == enhetHsaId && v.Giltighet.Överlappar(giltighet)))
            Throw.Vårdval.ÖverlappEjTillåtet(enhetHsaId, giltighet);

        var aktivt = AktivtVårdval;

        if (aktivt is not null)
        {
            if (giltighet.Start < aktivt.Giltighet.Start)
                Throw.Vårdval.SlutFöreStart(aktivt.Giltighet.Start, giltighet.Start);

            AvslutaAktuelltVårdval(DateOnly.FromDateTime(giltighet.Start.DateTime));
        }

        var nytt = Domain.Vårdval.Skapa(Id, enhetHsaId, giltighet, nu);
        Vårdval.Add(nytt);

        Raise(new VårdvalSkapat(RegionId, Id, Personnummer, nu));

        return nytt;
    }

    public sealed record VårdvalSkapat(RegionId RegionId, PersonId PersonId, Personnummer Personnummer, DateTimeOffset OccurredAt) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
    }
}