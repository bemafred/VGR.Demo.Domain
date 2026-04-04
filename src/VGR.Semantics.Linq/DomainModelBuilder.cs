using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using VGR.Semantics.Abstractions;

namespace VGR.Semantics.Linq;

/// <summary>
/// Bygger <see cref="DomainModel"/> via reflection mot domän-assemblies.
/// Klassificerar typer baserat på strukturella heuristiker.
/// </summary>
internal static class DomainModelBuilder
{
    private static readonly HashSet<string> SkipMethods =
    [
        "Equals", "GetHashCode", "ToString", "GetType",
        "Deconstruct", "PrintMembers", "<Clone>$"
    ];

    private static readonly HashSet<string> SkipProperties =
    [
        "EqualityContract"
    ];

    public static DomainModel Build(
        Assembly[] assemblies,
        ConcurrentDictionary<MethodInfo, LambdaExpression> semanticRegistry)
    {
        var semanticMethods = new HashSet<MethodInfo>(semanticRegistry.Keys);
        var types = new List<DomainType>();

        foreach (var assembly in assemblies)
        {
            Type[] exportedTypes;
            try { exportedTypes = assembly.GetExportedTypes(); }
            catch (ReflectionTypeLoadException e)
            {
                exportedTypes = e.Types.Where(t => t is not null).ToArray()!;
            }

            foreach (var type in exportedTypes)
            {
                if (type.IsEnum) continue;
                if (type.IsInterface) continue;
                if (type.Name.StartsWith('<')) continue; // compiler-generated

                var kind = Classify(type);
                var properties = ExtractProperties(type, semanticMethods);
                var methods = ExtractMethods(type, semanticMethods);

                types.Add(new DomainType(
                    type.Name,
                    type.FullName ?? type.Name,
                    kind,
                    properties,
                    methods));
            }
        }

        // Sortera: aggregat först, sedan entiteter, värdeobjekt, etc.
        types.Sort((a, b) => a.Kind != b.Kind
            ? a.Kind.CompareTo(b.Kind)
            : string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        return new DomainModel(types);
    }

    private static DomainTypeKind Classify(Type type)
    {
        // DomainEvent: implementerar IDomainEvent eller ärver DomainEvent
        if (type.GetInterfaces().Any(i => i.Name == "IDomainEvent"))
            return DomainTypeKind.DomainEvent;

        // Exception: ärver från Exception
        if (typeof(Exception).IsAssignableFrom(type))
            return DomainTypeKind.Exception;

        // Identity: readonly record struct med Guid Value + Nytt()
        if (type.IsValueType && IsRecordStruct(type))
        {
            var hasGuidValue = type.GetProperty("Value")?.PropertyType == typeof(Guid);
            var hasNytt = type.GetMethod("Nytt", BindingFlags.Public | BindingFlags.Static) is not null;
            if (hasGuidValue && hasNytt)
                return DomainTypeKind.Identity;

            return DomainTypeKind.ValueObject;
        }

        // Static class: abstract + sealed (C# static class)
        if (type.IsAbstract && type.IsSealed)
            return DomainTypeKind.Static;

        // Aggregate: har DequeueEvents()
        if (type.GetMethod("DequeueEvents", BindingFlags.Public | BindingFlags.Instance) is not null)
            return DomainTypeKind.Aggregate;

        // Entity: sealed class med Id-property (inte aggregate)
        var idProp = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProp is not null && type.IsSealed)
            return DomainTypeKind.Entity;

        // Default: behandla som ValueObject om record, annars Static
        if (type.IsValueType || IsRecord(type))
            return DomainTypeKind.ValueObject;

        return DomainTypeKind.Static;
    }

    private static IReadOnlyList<DomainProperty> ExtractProperties(
        Type type, HashSet<MethodInfo> semanticMethods)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => !SkipProperties.Contains(p.Name))
            .Select(p =>
            {
                var getter = p.GetGetMethod(true);
                var hasSemantic = getter is not null && semanticMethods.Contains(getter);
                var isReadOnly = p.SetMethod is null || !p.SetMethod.IsPublic;
                return new DomainProperty(
                    p.Name,
                    FormatTypeName(p.PropertyType),
                    isReadOnly,
                    hasSemantic);
            })
            .ToList();

        // Statiska properties
        var staticProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(p => !SkipProperties.Contains(p.Name))
            .Select(p => new DomainProperty(
                p.Name,
                FormatTypeName(p.PropertyType),
                true,
                false));

        props.AddRange(staticProps);
        return props;
    }

    private static IReadOnlyList<DomainMethod> ExtractMethods(
        Type type, HashSet<MethodInfo> semanticMethods)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        return type.GetMethods(flags)
            .Where(m => !m.IsSpecialName) // skip property getters/setters, op_*
            .Where(m => !SkipMethods.Contains(m.Name))
            .Select(m => new DomainMethod(
                m.Name,
                m.GetParameters()
                    .Select(p => new DomainParameter(p.Name ?? "_", FormatTypeName(p.ParameterType)))
                    .ToList(),
                FormatTypeName(m.ReturnType),
                m.IsStatic,
                semanticMethods.Contains(m)))
            .ToList();
    }

    private static string FormatTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(byte[])) return "byte[]";

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return FormatTypeName(underlying) + "?";

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));

            if (def == typeof(IReadOnlyList<>)) return $"IReadOnlyList<{args}>";
            if (def == typeof(IEnumerable<>)) return $"IEnumerable<{args}>";
            if (def == typeof(List<>)) return $"List<{args}>";

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick > 0) name = name[..tick];
            return $"{name}<{args}>";
        }

        return type.Name;
    }

    private static bool IsRecordStruct(Type type) =>
        type.IsValueType &&
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;
}
