using System.Reflection;

namespace VGR.Technical.Web;

/// <summary>
/// Läser embedded resources från VGR.Technical.Web-assemblyn.
/// </summary>
internal static class EmbeddedAssets
{
    private static readonly Assembly Asm = typeof(EmbeddedAssets).Assembly;

    public static string Read(string logicalName)
    {
        using var stream = Asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded resource '{logicalName}' saknas.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
