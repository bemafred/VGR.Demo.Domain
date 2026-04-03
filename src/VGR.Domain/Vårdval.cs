using System.Linq.Expressions;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Abstractions;

namespace VGR.Domain;

/// <summary>Entitet som representerar en persons vårdval vid en vårdenhet under en tidsperiod.</summary>
public sealed class Vårdval
{
    /// <summary>Unikt id för vårdvalet.</summary>
    public VårdvalId Id { get; private set; }
    /// <summary>Id för den person som vårdvalet tillhör.</summary>
    public PersonId PersonId { get; private set; }
    /// <summary>HSA-ID för den vårdenhet vårdvalet gäller på.</summary>
    public HsaId EnhetsHsaId { get; private set; }
    /// <summary>Giltighetstid: <c>[Start, Slut)</c>. Tillsvidare om <c>Slut</c> är <c>null</c>.</summary>
    public Tidsrymd Period { get; internal set; }
    /// <summary>Tidpunkt då vårdvalet skapades.</summary>
    public DateTimeOffset SkapadTid { get; private set; }
    /// <summary>Optimistisk samtidighetstoken.</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Sant om vårdvalet är aktivt (tillsvidare — saknar slutdatum).</summary>
    [SemanticQuery]
    public bool ÄrAktivt => Period.ÄrTillsvidare;

    /// <summary>Sant om vårdvalet är avslutat (har slutdatum).</summary>
    public bool ÄrAvslutat => !ÄrAktivt;

    private Vårdval() { }
    internal static Vårdval Skapa(PersonId personId, HsaId enhet, Tidsrymd giltighet, DateTimeOffset ts)
        => new() { Id = VårdvalId.Nytt(), PersonId = personId, EnhetsHsaId = enhet, Period = giltighet, SkapadTid = ts };

    /// <summary>Avslutar vårdvalet vid angiven tidpunkt. Kastar om slut ligger före start.</summary>
    public void Avsluta(DateTimeOffset slut)
    {
        if (slut < Period.Start)
            Throw.Vårdval.SlutFöreStart(Period.Start, slut);

        Period = Tidsrymd.Skapa(Period.Start, slut);
    }

    /// <summary>Avslutar vårdvalet vid angiven dag (lokal tidszon).</summary>
    public void Avsluta(DateOnly slut) => Avsluta(Tidsrymd.StartAvDag(slut, TimeZoneInfo.Local));
}