using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Dynamisk upplösning av DbContext utan kompileringstidsberoende till VGR.Infrastructure.EF.
/// Skannar AppDomain efter DbContext-subtyper och resolvar via IServiceProvider.
/// </summary>
internal static class DbContextAccessor
{
    private static Type? _readType;
    private static Type? _writeType;
    private static readonly object _lock = new();

    private static void EnsureDiscovered()
    {
        if (_readType is not null) return;
        lock (_lock)
        {
            if (_readType is not null) return;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name is null || name.StartsWith("System") || name.StartsWith("Microsoft") ||
                    name.StartsWith("netstandard") || name.StartsWith("mscorlib"))
                    continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t is not null).ToArray()!; }

                foreach (var t in types)
                {
                    if (t.IsAbstract || !t.IsSubclassOf(typeof(DbContext))) continue;

                    if (t.Name.Contains("Read", StringComparison.OrdinalIgnoreCase))
                        _readType = t;
                    else if (t.Name.Contains("Write", StringComparison.OrdinalIgnoreCase))
                        _writeType = t;
                }
            }
        }
    }

    public static DbContext GetReadContext(IServiceProvider sp)
    {
        EnsureDiscovered();
        return _readType is null
            ? throw new InvalidOperationException("Ingen ReadDbContext hittades.")
            : (DbContext)sp.GetService(_readType)!;
    }

    public static DbContext GetWriteContext(IServiceProvider sp)
    {
        EnsureDiscovered();
        return _writeType is null
            ? throw new InvalidOperationException("Ingen WriteDbContext hittades.")
            : (DbContext)sp.GetService(_writeType)!;
    }

    /// <summary>
    /// Hämtar alla entity-typer som är registrerade i DbContext:ens modell.
    /// Returnerar (typnamn → CLR-typ)-par.
    /// </summary>
    public static IReadOnlyList<Type> GetEntityTypes(DbContext ctx)
        => ctx.Model.GetEntityTypes().Select(e => e.ClrType).ToList();

    /// <summary>
    /// Hämtar en IQueryable för given CLR-typ via reflection på DbContext.Set&lt;T&gt;().
    /// </summary>
    private static readonly ConcurrentDictionary<Type, MethodInfo> _setMethods = new();

    public static IQueryable GetQueryable(DbContext ctx, Type entityType)
    {
        var setMethod = _setMethods.GetOrAdd(entityType, t =>
            typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(t));

        return (IQueryable)setMethod.Invoke(ctx, null)!;
    }

    /// <summary>
    /// Bygger en dynamisk Where-expression: entity => entity.Id == idValue
    /// </summary>
    public static IQueryable ApplyIdFilter(IQueryable query, Type entityType, object idValue)
    {
        var param = Expression.Parameter(entityType, "e");
        var idProp = entityType.GetProperty("Id")!;
        var member = Expression.Property(param, idProp);
        var constant = Expression.Constant(idValue, idProp.PropertyType);
        var equals = Expression.Equal(member, constant);
        var lambda = Expression.Lambda(equals, param);

        // Queryable.Where<T>(query, lambda)
        var whereMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2)
            .MakeGenericMethod(entityType);

        return (IQueryable)whereMethod.Invoke(null, [query, lambda])!;
    }

    /// <summary>
    /// Lägger till string-baserade Include() för alla navigationer.
    /// </summary>
    public static IQueryable ApplyIncludes(IQueryable query, Type entityType, DbContext ctx)
    {
        var entityTypeModel = ctx.Model.FindEntityType(entityType);
        if (entityTypeModel is null) return query;

        var includeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == "Include" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(string))
            .MakeGenericMethod(entityType);

        foreach (var nav in entityTypeModel.GetNavigations())
        {
            query = (IQueryable)includeMethod.Invoke(null, [query, nav.Name])!;
        }

        return query;
    }

    /// <summary>
    /// Kör FirstOrDefault dynamiskt på en IQueryable.
    /// </summary>
    public static object? FirstOrDefault(IQueryable query, Type entityType)
    {
        var method = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1)
            .MakeGenericMethod(entityType);

        return method.Invoke(null, [query]);
    }

    /// <summary>
    /// Kör ToList dynamiskt på en IQueryable.
    /// </summary>
    public static IList ToList(IQueryable query, Type entityType)
    {
        var method = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "ToList" && m.GetParameters().Length == 1)
            .MakeGenericMethod(entityType);

        return (IList)method.Invoke(null, [query])!;
    }

    /// <summary>
    /// Konstruerar ett identitets-värdeobjekt (t.ex. RegionId) från en Guid.
    /// </summary>
    public static object ConstructId(Type idType, Guid guid)
    {
        // Alla identity-typer är readonly record struct med (Guid Value)-konstruktor
        return Activator.CreateInstance(idType, guid)!;
    }
}
