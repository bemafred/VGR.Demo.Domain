
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
    /// <summary>Starttid (inkluderad).</summary>
    public DateTimeOffset Start { get; }

    /// <summary>Sluttid (exkluderad). <c>null</c> betyder tillsvidare (öppet slut).</summary>
    public DateTimeOffset? Slut  { get;  }

    [SemanticQuery]
    /// <summary>Sant om intervallet saknar slut (tillsvidare).</summary>
    public bool ÄrTillsvidare => Slut is null;

    /// <summary>Varaktighet: <c>Slut - Start</c>. Kastar om slut saknas.</summary>
    public TimeSpan Varaktighet
        => Slut is null ? throw new Exceptions.DomainUndefinedOperationException(
                              "Tidsrymd.VaraktighetOdefinieradFörTillsvidare",
                              "Varaktighet är odefinierad för tillsvidare-intervall.")
                        : Slut.Value - Start;

    /// <summary>Sant om intervallet är tomt (<c>Start == Slut</c>).</summary>
    public bool ÄrTomt => Slut is not null && Start == Slut.Value;

    /// <summary>Privat konstruktör. Använd fabrikerna.</summary>
    private Tidsrymd(DateTimeOffset start, DateTimeOffset? slut)
    {
        if (slut is { } e && e < start)
            Throw.Tidsrymd.SlutFöreStart(start, slut);

        Start = start;
        Slut  = slut;
    }

    /// <summary>Skapar ett tidsintervall från start och slut (slut är exkluderat).</summary>
    public static Tidsrymd Skapa(DateTimeOffset start, DateTimeOffset slut) 
        => new(start, slut);

    /// <summary>Skapar ett tidsintervall från start och varaktighet.</summary>
    public static Tidsrymd Skapa(DateTimeOffset start, TimeSpan varaktighet)
        => new(start, start + varaktighet);

    /// <summary>Skapar ett tillsvidare-intervall från start.</summary>
    public static Tidsrymd SkapaTillsvidare(DateTimeOffset start) 
        => new(start, null);

    /// <summary>Skapar ett tidsintervall för hela dagar i aktuell lokal tidszon: [start 00:00, slut 00:00).
    /// </summary>
    public static Tidsrymd Skapa(DateOnly start, DateOnly slut)
        => new(StartAvDag(start, TimeZoneInfo.Local), StartAvDag(slut, TimeZoneInfo.Local));

    /// <summary>
    /// Skapar ett tidsintervall för hela dagar i given tidszon (DST-hantering).
    /// </summary>
    public static Tidsrymd Skapa(DateOnly start, DateOnly slut, TimeZoneInfo tz)
        => new(StartAvDag(start, tz), StartAvDag(slut, tz));

    /// <summary>Skapar ett tidsintervall för hela dagar i aktuell lokal tidszon: [start 00:00, slut 00:00).
    /// Om <paramref name="slut"/> saknas skapas ett tillsvidare-intervall.
    /// </summary>
    public static Tidsrymd Skapa(DateOnly start, DateOnly? slut)
        => slut is { } s ? Skapa(start, s) : SkapaTillsvidare(start);

    /// <summary>
    /// Skapar ett tidsintervall för hela dagar i given tidszon (DST-hantering).
    /// Om <paramref name="slut"/> saknas skapas ett tillsvidare-intervall.
    /// </summary>
    public static Tidsrymd Skapa(DateOnly start, DateOnly? slut, TimeZoneInfo tz)
        => slut is { } s ? Skapa(start, s, tz) : SkapaTillsvidare(start, tz);

    /// <summary>Skapar ett tillsvidare-intervall för hela dagar i aktuell lokal tidszon.</summary>
    public static Tidsrymd SkapaTillsvidare(DateOnly start)
        => new(StartAvDag(start, TimeZoneInfo.Local), null);

    /// <summary>Skapar ett tillsvidare-intervall för hela dagar i given tidszon.</summary>
    public static Tidsrymd SkapaTillsvidare(DateOnly start, TimeZoneInfo tz)
        => new(StartAvDag(start, tz), null);

    /// <summary>True om tidpunkten <paramref name="t"/> ingår i intervallet.</summary>
    [SemanticQuery]
    public bool Innehåller(DateTimeOffset t) => Start <= t && (Slut is null || t < Slut.Value);

    /// <summary>True om tidpunkten <paramref name="t"/> (nullable) ingår i intervallet. <c>null</c> ger false.</summary>
    public bool Innehåller(DateTimeOffset? t) => t is { } v && Innehåller(v);

    /// <summary>Sant om två halvöppna intervall överlappar varandra.</summary>
    [SemanticQuery]
    public bool Överlappar(in Tidsrymd annan)
        => (annan.Slut is null || Start < annan.Slut.Value)
           && (Slut is null || annan.Start < Slut.Value);

    /// <summary>Snittet mellan två intervall, eller <c>null</c> om de inte överlappar.</summary>
    public Tidsrymd? Snitt(in Tidsrymd annan)
    {
        if (!Överlappar(annan)) return null;

        var s = Start > annan.Start ? Start : annan.Start;
        var e = MinNullable(Slut, annan.Slut);

        if (e is DateTimeOffset ce && s >= ce) return null; // tomt snitt
        return new(s, e);
    }

    /// <summary>True om intervallen överlappar eller är direkt angränsande.</summary>
    public bool ÖverlapparEllerAngränsar(in Tidsrymd annan)
    {
        var leftNonOverlap = Slut is { } a && a < annan.Start;
        var rightNonOverlap = annan.Slut is { } b && b < Start;
        return !(leftNonOverlap || rightNonOverlap);
    }

    /// <summary>Sammanfogar två intervall om de överlappar eller angränsar; annars <c>null</c>.</summary>
    public Tidsrymd? Sammanfoga(in Tidsrymd annan)
        => ÖverlapparEllerAngränsar(annan)
           ? new(Start < annan.Start ? Start : annan.Start,
                 MaxNullable(Slut, annan.Slut))
           : null;

    /// <summary>Delar intervallet vid givna brytpunkter (innanför <c>[Start, Slut)</c>).</summary>
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

    /// <summary>Returnerar delintervall med fast steg; sista korta biten kan inkluderas.</summary>
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

    /// <summary>Avger tidpunkter i jämna steg (t.ex. Varje timme) inom intervallet.</summary>
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

    /// <inheritdoc/>
    public override string ToString() => $"{Start:o}–{(Slut is DateTimeOffset e ? e.ToString("o") : "∞")}";

    /// <summary>Normaliserar: slår ihop överlapp/angränsningar till ett minimalt set.</summary>
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

    /// <summary>Minsta tidsintervall som täcker alla givna intervall.</summary>
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
    public bool VararLängreÄn(TimeSpan ts)
    {
        if (ÄrTillsvidare)
            return true;
        
        return Slut - Start > ts;
    }
    
    /// <summary>Skapar ett <see cref="Datumintervall"/> genom att kasta bort tid (behåller endast datumdelar).</summary>
    public Datumintervall TillDatumintervall()
    {
        if (ÄrTillsvidare)
            Throw.Tidsrymd.DatumintervallKräverAvgränsatIntervall();
        return Datumintervall.FrånTidsrymd(this);
    }

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
