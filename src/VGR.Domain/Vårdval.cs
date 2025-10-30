using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

public sealed class Vårdval
{
    public VardvalId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public HsaId EnhetsHsaId { get; private set; }
    public Tidsrymd Giltighet { get; private set; }
    public DateTimeOffset SkapadTid { get; private set; }

    private Vårdval() { }
    internal static Vårdval Skapa(VardvalId id, PersonId personId, HsaId enhet, Tidsrymd giltighet, DateTimeOffset ts)
        => new() { Id = id, PersonId = personId, EnhetsHsaId = enhet, Giltighet = giltighet, SkapadTid = ts };

    public void Avsluta(DateTimeOffset slut)
    {
        if (slut < Giltighet.Start) Throw.Vårdval.SlutFöreStart(Giltighet.Start, slut);
        Giltighet = Tidsrymd.Skapa(Giltighet.Start, slut);
    }

    public void Avsluta(DateOnly slut) => Avsluta(new DateTimeOffset(slut, TimeOnly.MinValue, TimeSpan.Zero));
}