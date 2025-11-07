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

    public Vårdval SkapaVårdval(HsaId enhetHsaId, Tidsrymd giltighet, DateTimeOffset nu)
    {
        var aktivt = AktivtVårdval;

        if (Vårdval.Any(v => v.EnhetsHsaId == enhetHsaId && v.Giltighet.Överlappar(giltighet)))
            Throw.Vårdval.ÖverlappEjTillåtet(enhetHsaId, giltighet);

        if (aktivt is not null)
        {
            if (giltighet.Start < aktivt.Giltighet.Start)
                Throw.Vårdval.SlutFöreStart(aktivt.Giltighet.Start, giltighet.Start);

            if (aktivt.EnhetsHsaId == enhetHsaId && aktivt.Giltighet.Start == giltighet.Start)
                Throw.Vårdval.AktivtFinnsRedan();

            aktivt.Avsluta(giltighet.Start);
        }

        var vv = Domain.Vårdval.Skapa(Id, enhetHsaId, giltighet, nu);
        Vårdval.Add(vv);

        Raise(new VårdvalSkapat(RegionId, Id, Personnummer, nu));

        return vv;
    }

    public sealed record VårdvalSkapat(RegionId RegionId, PersonId PersonId, Personnummer Personnummer, DateTimeOffset OccurredAt) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
    }
}