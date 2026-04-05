using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Konverterar JSON-element till CLR-typer för domänmetod-invocation.
/// Hanterar primitiver, identitetstyper, värdeobjekt med Tolka/FörsökTolka, och sammansatta typer.
/// </summary>
internal static class ParameterConverter
{
    public static object?[] ConvertParameters(ParameterInfo[] parameters, JsonElement json)
    {
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (json.TryGetProperty(param.Name!, out var element))
                args[i] = ConvertValue(element, param.ParameterType);
            else if (param.HasDefaultValue)
                args[i] = param.DefaultValue;
            else
                throw new ArgumentException($"Parametern '{param.Name}' saknas i request-body.");
        }
        return args;
    }

    public static object? ConvertValue(JsonElement element, Type targetType)
    {
        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying is not null)
        {
            if (element.ValueKind == JsonValueKind.Null) return null;
            return ConvertValue(element, underlying);
        }

        // Primitiver
        if (targetType == typeof(string))
            return element.GetString();

        if (targetType == typeof(Guid))
            return element.ValueKind == JsonValueKind.String ? Guid.Parse(element.GetString()!) : element.GetGuid();

        if (targetType == typeof(int))
            return element.GetInt32();

        if (targetType == typeof(long))
            return element.GetInt64();

        if (targetType == typeof(double))
            return element.GetDouble();

        if (targetType == typeof(decimal))
            return element.GetDecimal();

        if (targetType == typeof(bool))
            return element.GetBoolean();

        if (targetType == typeof(DateTimeOffset))
            return element.ValueKind == JsonValueKind.String
                ? DateTimeOffset.Parse(element.GetString()!, CultureInfo.InvariantCulture)
                : element.GetDateTimeOffset();

        if (targetType == typeof(DateOnly))
            return DateOnly.Parse(element.GetString()!, CultureInfo.InvariantCulture);

        if (targetType == typeof(TimeSpan))
            return TimeSpan.Parse(element.GetString()!, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateTime))
            return element.GetDateTime();

        // Identity-typer: readonly record struct med (Guid Value)-konstruktor
        if (targetType.IsValueType && targetType.GetProperty("Value") is { PropertyType.Name: "Guid" })
        {
            var guidStr = element.ValueKind == JsonValueKind.String ? element.GetString()! : element.ToString();
            var guid = Guid.Parse(guidStr);
            return Activator.CreateInstance(targetType, guid);
        }

        // Värdeobjekt med Tolka(string): HsaId, Personnummer etc.
        var tolkaMethod = targetType.GetMethod("Tolka", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
        if (tolkaMethod is not null && element.ValueKind == JsonValueKind.String)
            return tolkaMethod.Invoke(null, [element.GetString()!]);

        // Sammansatta värdeobjekt med Skapa-fabriker: Tidsrymd etc.
        if (element.ValueKind == JsonValueKind.Object)
            return ConvertComplexValue(element, targetType);

        // Fallback: försök med System.Text.Json
        return JsonSerializer.Deserialize(element.GetRawText(), targetType);
    }

    private static object? ConvertComplexValue(JsonElement element, Type targetType)
    {
        // Tidsrymd: { "start": "...", "slut": "..." } eller { "start": "..." } (tillsvidare)
        if (targetType.Name == "Tidsrymd")
        {
            if (element.TryGetProperty("start", out var startEl) || element.TryGetProperty("Start", out startEl))
            {
                var start = DateTimeOffset.Parse(startEl.GetString()!, CultureInfo.InvariantCulture);

                var hasSlut = element.TryGetProperty("slut", out var slutEl) || element.TryGetProperty("Slut", out slutEl);
                if (hasSlut && slutEl.ValueKind != JsonValueKind.Null)
                {
                    var slut = DateTimeOffset.Parse(slutEl.GetString()!, CultureInfo.InvariantCulture);
                    var skapaMethod = targetType.GetMethod("Skapa", BindingFlags.Public | BindingFlags.Static,
                        [typeof(DateTimeOffset), typeof(DateTimeOffset)]);
                    return skapaMethod!.Invoke(null, [start, slut]);
                }
                else
                {
                    var tillsvidareMethod = targetType.GetMethod("SkapaTillsvidare", BindingFlags.Public | BindingFlags.Static,
                        [typeof(DateTimeOffset)]);
                    return tillsvidareMethod!.Invoke(null, [start]);
                }
            }
        }

        // Generell: försök hitta en Skapa-fabrik som matchar JSON-properties
        var skapaMethodes = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "Skapa")
            .OrderByDescending(m => m.GetParameters().Length)
            .ToList();

        foreach (var method in skapaMethodes)
        {
            var methodParams = method.GetParameters();
            var match = methodParams.All(p =>
                element.TryGetProperty(p.Name!, out _) ||
                element.TryGetProperty(char.ToUpper(p.Name![0]) + p.Name[1..], out _));

            if (match)
            {
                var args = new object?[methodParams.Length];
                for (var i = 0; i < methodParams.Length; i++)
                {
                    var p = methodParams[i];
                    if (!element.TryGetProperty(p.Name!, out var el))
                        element.TryGetProperty(char.ToUpper(p.Name![0]) + p.Name[1..], out el);
                    args[i] = ConvertValue(el, p.ParameterType);
                }
                return method.Invoke(null, args);
            }
        }

        // Fallback
        return JsonSerializer.Deserialize(element.GetRawText(), targetType);
    }
}
