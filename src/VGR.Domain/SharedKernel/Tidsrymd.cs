
using System.Collections.Generic;
using System.Linq;

using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Abstractions;

namespace VGR.Domain.SharedKernel;
/// <summary>
/// Ett exakt tidsintervall med halvöppen semantik: <c>[Start, Slut)</c>.
/// <para><b>Start ingår.</b> <b>Slut ingår inte.</b> Krav: <c>Slut ≥ Start</c> (om slut finns).</para>
/// </summary>
public readonly record struct Tidsrymd
{
    /// <summary>Starttid (inkluderad i intervallet).</summary>
    public DateTimeOffset Start { get; }

    /// <summary>Sluttid (exkluderad från intervallet). <c>null</c> betyder tillsvidare (öppet slut).</summary>
    public DateTimeOffset? Slut  { get;  }

    /// <summary>Sant om intervallet saknar slut (tillsvidare).</summary>
    [SemanticQuery]
    public bool ÄrTillsvidare => Slut is null;

    /// <summary>Varaktighet: <c>Slut - Start</c>.</summary>
    /// <exception cref="Exceptions.DomainUndefinedOperationException">
    /// Kastas om intervallet är tillsvidare (slut saknas).
    /// </exception>
    public TimeSpan Varaktighet
        => Slut is null ? throw new Exceptions.DomainUndefinedOperationException(
                              "Tidsrymd.VaraktighetOdefinieradFörTillsvidare",
                              "Varaktighet är odefinierad för tillsvidare-intervall.")
                        : Slut.Value - Start;

    /// <summary>Sant om intervallet är tomt (<c>Start == Slut</c>).</summary>
    public bool ÄrTomt => Slut is not null && Start == Slut.Value;

    /// <summary>
    /// Privat konstruktör. Använd fabrikerna (<see cref="Skapa(DateTimeOffset, DateTimeOffset)"/> m.fl.).
    /// Validerar att <c>Slut ≥ Start</c> när slut är angivet.
    /// </summary>
    private Tidsrymd(DateTimeOffset start, DateTimeOffset? slut)
    {
        if (slut is { } e && e < start)
            Throw.Tidsrymd.SlutFöreStart(start, slut);

        Start = start;
        Slut  = slut;
    }

    /// <summary>Skapar ett tidsintervall från start och slut (slut är exkluderat).</summary>
    /// <param name="start">Starttid (inkluderad).</param>
    /// <param name="slut">Sluttid (exkluderad). Måste vara <c>≥ start</c>.</param>
    public static Tidsrymd Skapa(DateTimeOffset start, DateTimeOffset slut)
        => new(start, slut);

    /// <summary>Skapar ett tidsintervall från start och varaktighet.</summary>
    /// <param name="start">Starttid (inkluderad).</param>
    /// <param name="varaktighet">Längden på intervallet. Måste vara <c>≥ TimeSpan.Zero</c>.</param>
    public static Tidsrymd Skapa(DateTimeOffset start, TimeSpan varaktighet)
        => new(start, start + varaktighet);

    /// <summary>Skapar ett tillsvidare-intervall (öppet slut) från start.</summary>
    /// <param name="start">Starttid (inkluderad).</param>
    public static Tidsrymd SkapaTillsvidare(DateTimeOffset start)
        => new(start, null);

    /// <summary>
    /// Skapar ett tidsintervall för hela dagar i aktuell lokal tidszon: <c>[start 00:00, slut 00:00)</c>.
    /// </summary>
    /// <remarks>
    /// Använder <see cref="TimeZoneInfo.Local"/> vilket gör beteendet maskinberoende. Föredra
    /// <see cref="Skapa(DateOnly, DateOnly, TimeZoneInfo)"/> i kod som behöver vara deterministisk
    /// (t.ex. tester, bakgrundsjobb, serverkod som kör i UTC).
    /// </remarks>
    /// <param name="start">Startdatum (00:00 lokal tid inkluderas).</param>
    /// <param name="slut">Slutdatum (00:00 lokal tid exkluderas).</param>
    public static Tidsrymd Skapa(DateOnly start, DateOnly slut)
        => new(StartAvDag(start, TimeZoneInfo.Local), StartAvDag(slut, TimeZoneInfo.Local));

    /// <summary>
    /// Skapar ett tidsintervall för hela dagar i given tidszon (med DST-hantering).
    /// </summary>
    /// <param name="start">Startdatum (00:00 i <paramref name="tz"/> inkluderas).</param>
    /// <param name="slut">Slutdatum (00:00 i <paramref name="tz"/> exkluderas).</param>
    /// <param name="tz">Tidszon som styr när dagen börjar (t.ex. <c>Europe/Stockholm</c>).</param>
    public static Tidsrymd Skapa(DateOnly start, DateOnly slut, TimeZoneInfo tz)
        => new(StartAvDag(start, tz), StartAvDag(slut, tz));

    /// <summary>
    /// Skapar ett tidsintervall för hela dagar i aktuell lokal tidszon: <c>[start 00:00, slut 00:00)</c>.
    /// Om <paramref name="slut"/> saknas skapas ett tillsvidare-intervall.
    /// </summary>
    /// <remarks>Se <see cref="Skapa(DateOnly, DateOnly)"/> för anmärkning om <see cref="TimeZoneInfo.Local"/>.</remarks>
    /// <param name="start">Startdatum (00:00 lokal tid inkluderas).</param>
    /// <param name="slut">Slutdatum eller <c>null</c> för tillsvidare.</param>
    public static Tidsrymd Skapa(DateOnly start, DateOnly? slut)
        => slut is { } s ? Skapa(start, s) : SkapaTillsvidare(start);

    /// <summary>
    /// Skapar ett tidsintervall för hela dagar i given tidszon (med DST-hantering).
    /// Om <paramref name="slut"/> saknas skapas ett tillsvidare-intervall.
    /// </summary>
    /// <param name="start">Startdatum (00:00 i <paramref name="tz"/> inkluderas).</param>
    /// <param name="slut">Slutdatum eller <c>null</c> för tillsvidare.</param>
    /// <param name="tz">Tidszon som styr när dagen börjar.</param>
    public static Tidsrymd Skapa(DateOnly start, DateOnly? slut, TimeZoneInfo tz)
        => slut is { } s ? Skapa(start, s, tz) : SkapaTillsvidare(start, tz);

    /// <summary>Skapar ett tillsvidare-intervall för hela dagar i aktuell lokal tidszon.</summary>
    /// <remarks>Se <see cref="Skapa(DateOnly, DateOnly)"/> för anmärkning om <see cref="TimeZoneInfo.Local"/>.</remarks>
    /// <param name="start">Startdatum (00:00 lokal tid inkluderas).</param>
    public static Tidsrymd SkapaTillsvidare(DateOnly start)
        => new(StartAvDag(start, TimeZoneInfo.Local), null);

    /// <summary>Skapar ett tillsvidare-intervall för hela dagar i given tidszon.</summary>
    /// <param name="start">Startdatum (00:00 i <paramref name="tz"/> inkluderas).</param>
    /// <param name="tz">Tidszon som styr när dagen börjar.</param>
    public static Tidsrymd SkapaTillsvidare(DateOnly start, TimeZoneInfo tz)
        => new(StartAvDag(start, tz), null);

    /// <summary>Sant om tidpunkten <paramref name="t"/> ingår i intervallet.</summary>
    /// <param name="t">Tidpunkt att testa.</param>
    [SemanticQuery]
    public bool Innehåller(DateTimeOffset t) => Start <= t && (Slut is null || t < Slut.Value);

    /// <summary>Sant om tidpunkten <paramref name="t"/> ingår i intervallet. <c>null</c> ger <c>false</c>.</summary>
    /// <param name="t">Tidpunkt att testa, eller <c>null</c>.</param>
    public bool Innehåller(DateTimeOffset? t) => t is { } v && Innehåller(v);

    /// <summary>Sant om två halvöppna intervall överlappar varandra.</summary>
    /// <param name="annan">Det andra intervallet.</param>
    [SemanticQuery]
    public bool Överlappar(in Tidsrymd annan)
        => (annan.Slut is null || Start < annan.Slut.Value)
           && (Slut is null || annan.Start < Slut.Value);

    /// <summary>Beräknar snittet mellan två intervall.</summary>
    /// <param name="annan">Det andra intervallet.</param>
    /// <returns>Snittintervallet, eller <c>null</c> om intervallen inte överlappar (eller om snittet är tomt).</returns>
    public Tidsrymd? Snitt(in Tidsrymd annan)
    {
        if (!Överlappar(annan)) return null;

        var s = Start > annan.Start ? Start : annan.Start;
        var e = MinNullable(Slut, annan.Slut);

        if (e is DateTimeOffset ce && s >= ce) return null; // tomt snitt
        return new(s, e);
    }

    /// <summary>Sant om intervallen överlappar eller är direkt angränsande.</summary>
    /// <param name="annan">Det andra intervallet.</param>
    public bool ÖverlapparEllerAngränsar(in Tidsrymd annan)
    {
        var leftNonOverlap = Slut is { } a && a < annan.Start;
        var rightNonOverlap = annan.Slut is { } b && b < Start;
        return !(leftNonOverlap || rightNonOverlap);
    }

    /// <summary>Sammanfogar två intervall om de överlappar eller angränsar.</summary>
    /// <param name="annan">Det andra intervallet.</param>
    /// <returns>Det sammanfogade intervallet, eller <c>null</c> om intervallen är åtskilda.</returns>
    public Tidsrymd? Sammanfoga(in Tidsrymd annan)
        => ÖverlapparEllerAngränsar(annan)
           ? new(Start < annan.Start ? Start : annan.Start,
                 MaxNullable(Slut, annan.Slut))
           : null;

    /// <summary>Delar intervallet vid givna brytpunkter (endast brytpunkter strikt innanför <c>[Start, Slut)</c> beaktas).</summary>
    /// <param name="brytpunkter">Tidpunkter att dela vid. Dubbletter och punkter utanför intervallet ignoreras.</param>
    /// <returns>Delintervallen i kronologisk ordning. Tomma intervall ger en tom sekvens.</returns>
    public IEnumerable<Tidsrymd> DelaVid(params DateTimeOffset[] brytpunkter)
    {
        if (ÄrTomt) yield break;

        var start = Start;
        var slutLocal = Slut ?? DateTimeOffset.MaxValue;

        var cuts = brytpunkter
            .Where(p => start < p && p < slutLocal)
            .Distinct()
            .OrderBy(p => p);

        var s = Start;
        foreach (var c in cuts)
        {
            yield return Skapa(s, c);
            s = c;
        }

        yield return Slut is DateTimeOffset e ? Skapa(s, e) : new Tidsrymd(s, null);
    }

    /// <summary>Returnerar delintervall med fast steg.</summary>
    /// <param name="steg">Steglängd. Måste vara strikt positiv.</param>
    /// <param name="inkluderaSistaKortare">
    /// Om <c>true</c> (standard) returneras en sista kortare bit när intervallet inte delas jämnt av <paramref name="steg"/>.
    /// Om <c>false</c> utelämnas restbiten.
    /// </param>
    /// <returns>Delintervallen i kronologisk ordning.</returns>
    /// <exception cref="Exceptions.DomainValidationException">Kastas om <paramref name="steg"/> inte är strikt positiv.</exception>
    /// <exception cref="Exceptions.DomainUndefinedOperationException">Kastas om intervallet är tillsvidare.</exception>
    public IEnumerable<Tidsrymd> Stega(TimeSpan steg, bool inkluderaSistaKortare = true)
    {
        if (steg <= TimeSpan.Zero)
            Throw.Tidsrymd.StegMåsteVaraPositivt(steg);
        if (ÄrTillsvidare)
            Throw.Tidsrymd.StegningKräverAvgränsatIntervall();

        var cursor = Start;
        while (cursor + steg <= Slut!.Value)
        {
            yield return Skapa(cursor, steg);
            cursor += steg;
        }

        if (inkluderaSistaKortare && cursor < Slut!.Value)
            yield return Skapa(cursor, Slut!.Value);
    }

    /// <summary>Avger tidpunkter i jämna steg (t.ex. varje timme) inom intervallet.</summary>
    /// <param name="steg">Avstånd mellan tidpunkter. Måste vara strikt positiv.</param>
    /// <param name="inkluderaSlut">Om <c>true</c> tas <see cref="Slut"/>-punkten med som sista element.</param>
    /// <returns>Tidpunkterna i kronologisk ordning, med start från <see cref="Start"/>.</returns>
    /// <exception cref="Exceptions.DomainValidationException">Kastas om <paramref name="steg"/> inte är strikt positiv.</exception>
    /// <exception cref="Exceptions.DomainUndefinedOperationException">Kastas om intervallet är tillsvidare.</exception>
    public IEnumerable<DateTimeOffset> Varje(TimeSpan steg, bool inkluderaSlut = false)
    {
        if (steg <= TimeSpan.Zero)
            Throw.Tidsrymd.StegMåsteVaraPositivt(steg);
        if (ÄrTillsvidare)
            Throw.Tidsrymd.EnumereringKräverAvgränsatIntervall();

        for (var t = Start; t < Slut!.Value; t += steg)
            yield return t;

        if (inkluderaSlut) yield return Slut!.Value;
    }

    /// <summary>
    /// Returnerar en strängrepresentation på formen <c>{Start:o}–{Slut:o}</c> (ISO 8601 round-trip),
    /// där öppet slut visas som <c>∞</c>.
    /// </summary>
    public override string ToString() => $"{Start:o}–{(Slut is DateTimeOffset e ? e.ToString("o") : "∞")}";

    /// <summary>Normaliserar en samling intervall: slår ihop överlapp och angränsningar till ett minimalt set.</summary>
    /// <param name="intervall">Intervallen som ska normaliseras (godtycklig ordning).</param>
    /// <returns>De sammanslagna intervallen i kronologisk ordning. Returnerar inmatningen oförändrad om den innehåller ≤ 1 element.</returns>
    public static IReadOnlyList<Tidsrymd> Normalisera(IEnumerable<Tidsrymd> intervall)
    {
        var list = intervall.OrderBy(x => x.Start).ToList();
        if (list.Count <= 1) return list;

        var stack = new List<Tidsrymd>(list.Count);
        var current = list[0];
        for (var i = 1; i < list.Count; i++)
        {
            var next = list[i];
            var fusion = current.Sammanfoga(next);
            if (fusion is { } fused) current = fused;
            else { stack.Add(current); current = next; }
        }
        stack.Add(current);
        return stack;
    }

    /// <summary>Beräknar det minsta tidsintervall som täcker alla givna intervall.</summary>
    /// <param name="intervall">Intervallen som ska omslutas. Måste innehålla minst ett element.</param>
    /// <returns>
    /// Ett intervall från tidigaste <see cref="Start"/> till senaste <see cref="Slut"/>.
    /// Om något intervall är tillsvidare blir det returnerade intervallet också tillsvidare.
    /// </returns>
    /// <exception cref="Exceptions.DomainUndefinedOperationException">Kastas om <paramref name="intervall"/> är tom.</exception>
    public static Tidsrymd Omfång(IEnumerable<Tidsrymd> intervall)
    {
        var enumerated = intervall as IList<Tidsrymd> ?? intervall.ToList();
        if (enumerated.Count == 0) Throw.Tidsrymd.OmfångKräverMinstEttIntervall();

        var min = enumerated.Min(x => x.Start);
        DateTimeOffset? max = enumerated.Any(x => x.Slut is null)
            ? null
            : enumerated.Max(x => x.Slut!.Value);

        return new(min, max);
    }

    /// <summary>Sant om intervallet varar längre än angiven tidslängd. Tillsvidare-intervall ger alltid <c>true</c>.</summary>
    /// <param name="ts">Tidslängd att jämföra mot.</param>
    public bool VararLängreÄn(TimeSpan ts)
    {
        if (ÄrTillsvidare)
            return true;

        return Slut - Start > ts;
    }

    /// <summary>Skapar ett <see cref="Datumintervall"/> genom att kasta bort tidsdelen (behåller endast datum).</summary>
    /// <returns>Motsvarande datumintervall.</returns>
    /// <exception cref="Exceptions.DomainUndefinedOperationException">Kastas om intervallet är tillsvidare.</exception>
    public Datumintervall TillDatumintervall()
    {
        if (ÄrTillsvidare)
            Throw.Tidsrymd.DatumintervallKräverAvgränsatIntervall();
        return Datumintervall.FrånTidsrymd(this);
    }

    /// <summary>
    /// Beräknar tidpunkten för dagens början (00:00) för ett givet datum i en given tidszon.
    /// </summary>
    /// <remarks>
    /// Hanterar DST-övergångar deterministiskt:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Ogiltig tid (spring-forward):</b> om midnatt inte existerar (sällsynt — vissa tidszoner som inte finns i Sverige)
    ///     flyttas tiden framåt i 15-minuterssteg tills den blir giltig (max 3 h).
    ///   </description></item>
    ///   <item><description>
    ///     <b>Tvetydig tid (fall-back):</b> om midnatt förekommer två gånger väljs den tidigaste UTC-instansen
    ///     (störst offset mot UTC).
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <param name="dag">Datum vars start ska beräknas.</param>
    /// <param name="tz">Tidszon som styr när dagen börjar.</param>
    /// <returns>Tidpunkten 00:00 lokal tid i <paramref name="tz"/> som <see cref="DateTimeOffset"/>.</returns>
    public static DateTimeOffset StartAvDag(DateOnly dag, TimeZoneInfo tz)
    {
        // Lokal midnatt (okänd kind så att tz-regler gäller)
        var local = new DateTime(dag.Year, dag.Month, dag.Day, 0, 0, 0, DateTimeKind.Unspecified);

        // Om midnatt är ogiltig (sällsynt), flytta fram till första giltiga tid.
        if (tz.IsInvalidTime(local))
        {
            var probe = local;
            for (var i = 0; i < 12 && tz.IsInvalidTime(probe); i++) // upp till 3h framåt i 15-min steg
                probe = probe.AddMinutes(15);

            var off = tz.GetUtcOffset(probe);
            return new DateTimeOffset(probe, off);
        }

        // Om midnatt är tvetydig (fall-back), välj den tidigaste möjliga UTC-instansen (största offset).
        if (tz.IsAmbiguousTime(local))
        {
            var offs = tz.GetAmbiguousTimeOffsets(local);
            var chosen = offs[0] > offs[1] ? offs[0] : offs[1];
            return new DateTimeOffset(local, chosen);
        }

        // Normalfall
        var offset = tz.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }

    // Min/Max över nullable slutpunkter där null representerar ∞ (öppet slut).
    // Min(null, x) = x  (∞ är aldrig minst).
    // Max(null, _) = null (∞ vinner alltid).
    private static DateTimeOffset? MinNullable(DateTimeOffset? a, DateTimeOffset? b)
        => (a, b) switch
        {
            (null, null) => null,
            (null, _)    => b,
            (_, null)    => a,
            _            => a.Value <= b.Value ? a : b
        };

    private static DateTimeOffset? MaxNullable(DateTimeOffset? a, DateTimeOffset? b)
        => (a, b) switch
        {
            (null, _)    => null,
            (_, null)    => null,
            _            => a.Value >= b.Value ? a : b
        };
}
