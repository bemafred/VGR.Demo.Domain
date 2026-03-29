using System.Linq.Expressions;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Abstractions;

namespace VGR.Domain;

public sealed class Vårdval
{
    public VårdvalId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public HsaId EnhetsHsaId { get; private set; }
    public Tidsrymd Period { get; internal set; }
    public DateTimeOffset SkapadTid { get; private set; }
    
    [SemanticQuery]
    public bool ÄrAktivt => Period.ÄrTillsvidare; 
    
    public bool ÄrAvslutat => !ÄrAktivt;

    private Vårdval() { }
    internal static Vårdval Skapa(PersonId personId, HsaId enhet, Tidsrymd giltighet, DateTimeOffset ts)
        => new() { Id = VårdvalId.Nytt(), PersonId = personId, EnhetsHsaId = enhet, Period = giltighet, SkapadTid = ts };

    public void Avsluta(DateTimeOffset slut)
    {
        if (slut < Period.Start) 
            Throw.Vårdval.SlutFöreStart(Period.Start, slut);

        Period = Tidsrymd.Skapa(Period.Start, slut);
    }

    public void Avsluta(DateOnly slut) => Avsluta(Tidsrymd.StartAvDag(slut, TimeZoneInfo.Local));
}