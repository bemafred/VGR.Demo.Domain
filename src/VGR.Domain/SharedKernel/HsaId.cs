using System.Text.RegularExpressions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.SharedKernel;

/// <summary>Värdeobjekt för HSA-ID (Hälso- och sjukvårdens adressregister).</summary>
public readonly record struct HsaId
{
    private static readonly Regex Rx = new(@"^[A-Z0-9-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    /// <summary>Kanonisk representation (versaler).</summary>
    public string Value { get; }
    private HsaId(string v) => Value = v;

    /// <summary>Tolkar ett HSA-ID från extern representation. Kastar vid ogiltigt format.</summary>
    public static HsaId Tolka(string input)
    {
        if (!FörsökTolka(input, out var v))
            Throw.HsaId.Ogiltigt(input);

        return v;
    }

    /// <summary>Försöker tolka ett HSA-ID. Returnerar <c>false</c> vid ogiltigt format.</summary>
    public static bool FörsökTolka(string? input, out HsaId id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var t = input.Trim();
        if (!Rx.IsMatch(t)) return false;
        id = new HsaId(t.ToUpperInvariant());
        return true;
    }

    /// <summary>Sant om HSA-ID:t avser en testenhet (innehåller 'T').</summary>
    public bool IsTest => Value.Contains('T');
    /// <inheritdoc/>
    public override string ToString() => Value;
    /// <summary>Implicit konvertering till sträng.</summary>
    public static implicit operator string(HsaId id) => id.Value;
}