namespace VGR.Domain.SharedKernel;

using System.Collections.Generic;
using VGR.Domain.SharedKernel.Exceptions;

/// <summary>
/// Ett kalenderintervall på datumnivå med halvöppen semantik: <c>[Start, Slut)</c>.
/// <para>
/// <b>Start ingår.</b> <b>Slut ingår inte.</b>
/// Om <see cref="Slut"/> är <c>null</c> tolkas intervallet som tillsvidare (öppet slut).
/// </para>
/// </summary>
/// <remarks>
/// Semantiken är avsedd för hela dagar. Använd <see cref="TillTidsrymd(TimeZoneInfo)"/> för att
/// skapa ett exakt tidsintervall (<see cref="Tidsrymd"/>) med korrekt hantering av tidszon/DST.
/// </remarks>
public readonly record struct Datumintervall
{
    /// <summary>
    /// Startdag (inkluderad).
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Slutdag (exkluderad). <c>null</c> betyder tillsvidare (öppet slut).
    /// </summary>
    public DateOnly? Slut { get; }

    /// <summary>
    /// Sant om intervallet saknar slut (tillsvidare).
    /// </summary>
    public bool ÄrTillsvidare => Slut is null;

    /// <summary>
    /// Sant om intervallet är tomt (<c>Start == Slut</c>). Aldrig sant för tillsvidare.
    /// </summary>
    public bool ÄrTom => Slut is not null && Start == Slut.Value;

    /// <summary>
    /// Privat konstruktör. Använd <see cref="Skapa(DateOnly, DateOnly)"/> eller <see cref="SkapaTillsvidare(DateOnly)"/>.
    /// </summary>
    /// <param name="start">Startdag (inkluderad).</param>
    /// <param name="slut">Slutdag (exkluderad) eller <c>null</c> för tillsvidare.</param>
    /// <exception cref="ArgumentOutOfRangeException">Om <paramref name="slut"/> &lt; <paramref name="start"/>.</exception>
    private Datumintervall(DateOnly start, DateOnly? slut)
    {
        if (slut is { } s && s < start)
            Throw.Datumintervall.SlutFöreStart(start, slut);
            
        Start = start;
        Slut = slut;
    }

    /// <summary>
    /// Skapar ett datumintervall från startdag och slutdag (slutdag är exkluderad).
    /// </summary>
    /// <param name="start">Startdag (inkluderad).</param>
    /// <param name="slut">Slutdag (exkluderad).</param>
    /// <returns>Ett nytt <see cref="Datumintervall"/>.</returns>
    public static Datumintervall Skapa(DateOnly start, DateOnly slut) => new(start, slut);

    /// <summary>
    /// Skapar ett datumintervall utan slut (tillsvidare).
    /// </summary>
    /// <param name="start">Startdag (inkluderad).</param>
    /// <returns>Ett nytt tillsvidare-<see cref="Datumintervall"/>.</returns>
    public static Datumintervall SkapaTillsvidare(DateOnly start) => new(start, null);

    /// <summary>
    /// Returnerar sant om dagen ingår i intervallet.
    /// </summary>
    /// <param name="dag">Dagen att testa.</param>
    /// <returns><c>true</c> om <paramref name="dag"/> ∈ <c>[Start, Slut)</c>, annars <c>false</c>.</returns>
    public bool Innehåller(DateOnly dag) => Start <= dag && (Slut is null || dag < Slut.Value);

    /// <summary>
    /// Återger varje dag i intervallet.
    /// </summary>
    /// <returns>Alla datum i <c>[Start, Slut)</c> i stigande ordning.</returns>
    /// <exception cref="InvalidOperationException">Om intervallet är tillsvidare.</exception>
    public IEnumerable<DateOnly> VarjeDag()
    {
        if (ÄrTillsvidare)
            Throw.Datumintervall.EnumereringKräverAvgränsatIntervall();

        for (var d = Start; d < Slut!.Value; d = d.AddDays(1))
            yield return d;
    }

    /// <summary>
    /// Skapar ett <see cref="Tidsrymd"/> som täcker detta datumintervall i vald tidszon.
    /// Använder tidszonens regler (inkl. Sommartid) för att slå upp midnatt.
    /// </summary>
    /// <param name="tz">Tidszon att tolka dagarnas midnatt i.</param>
    /// <returns>Ett <see cref="Tidsrymd"/> som motsvarar datumintervallet.</returns>
    public Tidsrymd TillTidsrymd(TimeZoneInfo tz)
        => ÄrTillsvidare
            ? Tidsrymd.SkapaTillsvidare(Start)
            : Tidsrymd.Skapa(Start, Slut!.Value, tz);

    /// <summary>
    /// Bygger ett <see cref="Datumintervall"/> från en <see cref="Tidsrymd"/> genom att använda lokala datumdelar.
    /// </summary>
    /// <param name="t">Tidsrymd som ska konverteras.</param>
    /// <returns>Ett motsvarande <see cref="Datumintervall"/>.</returns>
    public static Datumintervall FrånTidsrymd(Tidsrymd t)
        => t.Slut is null
            ? SkapaTillsvidare(DateOnly.FromDateTime(t.Start.LocalDateTime))
            : Skapa(DateOnly.FromDateTime(t.Start.LocalDateTime),
                    DateOnly.FromDateTime(t.Slut.Value.LocalDateTime));
}
