using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

public sealed class Person
{
    public PersonId Id { get; private init; }
    public Personnummer Personnummer { get; private set; }
    public DateTimeOffset SkapadTid { get; private set; }

    internal readonly List<Vårdval> Vårdval = [];
    public IReadOnlyList<Vårdval> AllaVårdval => Vårdval;

    private Person() { }
    public static Person Skapa(Personnummer pnr, DateTimeOffset nu)
        => new() { Id = new(), Personnummer = pnr, SkapadTid = nu };

    public Vårdval SkapaVårdval(HsaId enhetHsaId, Tidsrymd giltighet, DateTimeOffset nu)
    {
        if (Vårdval.Any(v => v.EnhetsHsaId == enhetHsaId && v.Giltighet.Överlappar(giltighet)))
            Throw.Vårdval.Överlapp(enhetHsaId, giltighet);

        var vv = Domain.Vårdval.Skapa(VardvalId.Nytt(), Id, enhetHsaId, giltighet, nu);
        Vårdval.Add(vv);
        return vv;
    }
}