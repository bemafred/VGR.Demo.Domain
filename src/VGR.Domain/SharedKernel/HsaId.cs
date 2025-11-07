using System.Text.RegularExpressions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.SharedKernel;

public readonly record struct HsaId
{
    private static readonly Regex Rx = new(@"^[A-Z0-9-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Value { get; }
    private HsaId(string v) => Value = v;

    public static HsaId Tolka(string input)
    {
        if (!FörsökTolka(input, out var v)) 
            Throw.HsaId.Ogiltigt(input);

        return v;
    }

    public static bool FörsökTolka(string? input, out HsaId id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var t = input.Trim();
        if (!Rx.IsMatch(t)) return false;
        id = new HsaId(t.ToUpperInvariant());
        return true;
    }

    public bool IsTest => Value.Contains('T');
    public override string ToString() => Value;
    public static implicit operator string(HsaId id) => id.Value;
}