namespace VGR.Domain.SharedKernel;

using System.Collections.Generic;
using System.Linq;

public static class TidsrymdAlgoritmer
{
    public static IEnumerable<Tidsrymd> Subtrahera(this Tidsrymd source, Tidsrymd cutter)
    {
        if (!source.Överlappar(cutter))
        {
            yield return source;
            yield break;
        }

        if (cutter.Start > source.Start)
            yield return Tidsrymd.Skapa(source.Start, cutter.Start);

        if (cutter.Slut.HasValue && source.Slut.HasValue && cutter.Slut.Value < source.Slut.Value)
            yield return Tidsrymd.Skapa(cutter.Slut.Value, source.Slut.Value);
    }

    public static IEnumerable<Tidsrymd> KlippMotFönster(IEnumerable<Tidsrymd> intervall, Tidsrymd fönster)
    {
        foreach (var i in intervall)
        {
            var snitt = i.Snitt(fönster);
            if (snitt is { } seg) yield return seg; 
        }
    }

    public static IReadOnlyList<Tidsrymd> FriaLuckor(in Tidsrymd fönster, IEnumerable<Tidsrymd> täckning)
    {
        var cut = Tidsrymd.Normalisera(KlippMotFönster(täckning, fönster));

        if (cut.Count == 0)
            return [fönster];

        var luckor = new List<Tidsrymd>();
        var cursor = fönster.Start;

        foreach (var block in cut)
        {
            if (cursor < block.Start)
                luckor.Add(Tidsrymd.Skapa(cursor, block.Start));

            if (!block.Slut.HasValue)
                return luckor;

            cursor = block.Slut.Value;
        }

        if (fönster.Slut.HasValue)
        {
            if (cursor < fönster.Slut.Value)
                luckor.Add(Tidsrymd.Skapa(cursor, fönster.Slut.Value));
        }
        else
        {
            luckor.Add(Tidsrymd.SkapaTillsvidare(cursor));
        }

        return luckor;
    }
}