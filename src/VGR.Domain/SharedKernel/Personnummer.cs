using System;
using System.Linq;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.SharedKernel;

/// <summary>
/// Värdeobjekt för svenskt personnummer.
/// Intern kanonisk representation: 12 siffror (YYYYMMDDXXXX).
/// </summary>
public readonly record struct Personnummer
{
    /// <summary>
    /// Kanonisk representation (12 siffror: YYYYMMDDXXXX).
    /// </summary>
    public string Value { get; }

    private Personnummer(string v) => Value = v;

    /// <summary>
    /// Parsar ett personnummer och normaliserar till 12 siffror.
    /// Kastar vid ogiltig inmatning.
    /// </summary>
    /// <param name="input">Inmatad sträng (kan innehålla '-' eller '+').</param>
    /// <exception cref="DomainArgumentFormatException">Om format eller datum är ogiltigt.</exception>
    public static Personnummer Parse(string input)
    {
        if (!TryParse(input, out var v))
            Throw.Personnummer.OgiltigtPersonnummer(input); 

        return v;
    }

    /// <summary>
    /// Försöker parsa personnummer (referensdatum: idag) och normaliserar till 12 siffror.
    /// </summary>
    /// <param name="input">Inmatad sträng (kan innehålla '-' eller '+').</param>
    /// <param name="p">Resultat om parsen lyckas (YYYYMMDDXXXX).</param>
    /// <returns>Sant vid lyckad parse.</returns>
    public static bool TryParse(string? input, out Personnummer p)
        => TryParse(input, DateOnly.FromDateTime(DateTime.Today), out p);

    /// <summary>
    /// Försöker parsa personnummer och normaliserar till 12 siffror.
    /// 10-siffriga värden (YYMMDDXXXX) får sekel beräknat utifrån <paramref name="referenceDate"/> och separator:
    /// '-' eller ingen separator => [-99, 0] år relativt referensåret; '+' => [-199, -100] år.
    /// </summary>
    /// <param name="input">Inmatad sträng (kan innehålla '-' eller '+').</param>
    /// <param name="referenceDate">Datum som styr sekeltolkning för 10-siffriga värden.</param>
    /// <param name="p">Resultat om parsen lyckas (YYYYMMDDXXXX).</param>
    /// <returns>Sant vid lyckad parse.</returns>
    public static bool TryParse(string? input, DateOnly referenceDate, out Personnummer p)
    {
        p = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var t = input.Trim();

        // Upptäck separatorer (styr sekeltolkning)
        char? sep = null;
        if (t.Contains('+')) sep = '+';
        else if (t.Contains('-')) sep = '-';

        // Rensa bort separatorer
        var digits = t.Replace("-", string.Empty).Replace("+", string.Empty);
        if (digits.Length != 10 && digits.Length != 12) return false;

        // Validera att allt är siffror
        if (digits.Any(ch => !char.IsDigit(ch)))
            return false;

        int year, month, day;

        if (digits.Length == 12)
        {
            // YYYYMMDDXXXX
            year = int.Parse(digits.AsSpan(0, 4));
            month = int.Parse(digits.AsSpan(4, 2));
            day = int.Parse(digits.AsSpan(6, 2));

            // Validera datum
            try { _ = new DateOnly(year, month, day); }
            catch (ArgumentOutOfRangeException) { return false; }

            // Redan 12-siffrig -> lagra kanonisk (utan separatorer)
            p = new Personnummer(digits);
        }
        else
        {
            // YYMMDDXXXX -> beräkna seklet
            var yy = int.Parse(digits.AsSpan(0, 2));
            month = int.Parse(digits.AsSpan(2, 2));
            day = int.Parse(digits.AsSpan(4, 2));
            year = ResolveYear(yy, sep, referenceDate);

            // Validera datum
            try { _ = new DateOnly(year, month, day); }
            catch (ArgumentOutOfRangeException) { return false; }

            // Bygg kanonisk 12-siffrig sträng: YYYY + MMDDXXXX
            var canonical = $"{year:D4}{digits[2..]}";
            p = new Personnummer(canonical);
        }

        return true;
    }

    /// <summary>
    /// Beräknar helt årtal baserat på tvåsiffrig årdel och separatorregler relativt referensdatum.
    /// </summary>
    private static int ResolveYear(int yy, char? sep, DateOnly referenceDate)
    {
        var centuryBase = (referenceDate.Year / 100) * 100;
        var candidate = centuryBase + yy;

        if (sep == '+')
        {
            // Placera i intervallet [Y-199, Y-100]
            var min = referenceDate.Year - 199;
            var max = referenceDate.Year - 100;
            while (candidate < min) candidate += 100;
            while (candidate > max) candidate -= 100;
        }
        else
        {
            // Placera i intervallet [Y-99, Y]
            var min = referenceDate.Year - 99;
            var max = referenceDate.Year;
            while (candidate < min) candidate += 100;
            while (candidate > max) candidate -= 100;
        }

        return candidate;
    }

    public override string ToString() => Value;
    public static implicit operator string(Personnummer p) => p.Value;
}