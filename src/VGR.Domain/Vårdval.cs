using System.Linq.Expressions;
using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

public sealed class Vårdval
{
    public static class Expression
    {
        public static readonly Expression<Func<Vårdval, bool>> ÄrÖppet
            = v => v.Giltighet.Slut == null;
    }

    public VårdvalId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public HsaId EnhetsHsaId { get; private set; }
    public Tidsrymd Giltighet { get; internal set; }
    public DateTimeOffset SkapadTid { get; private set; }
    public bool ÄrÖppet => Giltighet.ÄrTillsvidare; 
    public bool ÄrBegränsat => !ÄrÖppet;

    private Vårdval() { }
    internal static Vårdval Skapa(PersonId personId, HsaId enhet, Tidsrymd giltighet, DateTimeOffset ts)
        => new() { Id = new(), PersonId = personId, EnhetsHsaId = enhet, Giltighet = giltighet, SkapadTid = ts };

    public void Avsluta(DateTimeOffset slut)
    {
        if (slut < Giltighet.Start) 
            Throw.Vårdval.SlutFöreStart(Giltighet.Start, slut);

        Giltighet = Tidsrymd.Skapa(Giltighet.Start, slut);
    }

    public void Avsluta(DateOnly slut) => Avsluta(new DateTimeOffset(slut, TimeOnly.MinValue, TimeSpan.Zero));
}