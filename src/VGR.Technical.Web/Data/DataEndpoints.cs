using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VGR.Semantics.Abstractions;
using VGR.Semantics.Linq;
using VGR.Technical.Web.Mapping;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Registrerar <c>/data</c>-routes: aggregatlistning, instanser, detalj, relationer och mutation.
/// </summary>
internal static class DataEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // --- GET routes ---

        app.MapGet("/data", () =>
        {
            var model = SemanticRegistry.GetModel();
            return Results.Content(DataListPage.Render(model), "text/html");
        }).ExcludeFromDescription();

        app.MapGet("/data/{type}", (string type, HttpContext ctx) =>
        {
            var model = SemanticRegistry.GetModel();
            var domainType = model.Types.FirstOrDefault(t =>
                t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (domainType is null)
                return Results.NotFound($"Typen '{type}' hittades inte.");

            var clrType = ResolveClrType(domainType.FullName);
            if (clrType is null)
                return Results.NotFound($"CLR-typ '{domainType.FullName}' kunde inte resolvas.");

            using var dbCtx = DbContextAccessor.GetReadContext(ctx.RequestServices);
            var query = DbContextAccessor.GetQueryable(dbCtx, clrType);
            var instances = DbContextAccessor.ToList(query, clrType);

            return Results.Content(
                DataInstancesPage.Render(domainType.Name, domainType, instances, clrType),
                "text/html");
        }).ExcludeFromDescription();

        app.MapGet("/data/{type}/{id}", (string type, string id, HttpContext ctx) =>
        {
            var model = SemanticRegistry.GetModel();
            var domainType = model.Types.FirstOrDefault(t =>
                t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (domainType is null)
                return Results.NotFound($"Typen '{type}' hittades inte.");

            if (!Guid.TryParse(id, out var guid))
                return Results.BadRequest($"Ogiltigt id-format: '{id}'.");

            var clrType = ResolveClrType(domainType.FullName);
            if (clrType is null)
                return Results.NotFound($"CLR-typ '{domainType.FullName}' kunde inte resolvas.");

            using var dbCtx = DbContextAccessor.GetReadContext(ctx.RequestServices);
            var entity = LoadEntityById(dbCtx, clrType, guid);

            if (entity is null)
                return Results.NotFound($"{type} med id '{id}' hittades inte.");

            return Results.Content(
                DataDetailPage.Render(domainType.Name, id, entity, clrType, dbCtx),
                "text/html");
        }).ExcludeFromDescription();

        app.MapGet("/data/{type}/{id}/{relation}", (string type, string id, string relation, HttpContext ctx) =>
        {
            var model = SemanticRegistry.GetModel();
            var domainType = model.Types.FirstOrDefault(t =>
                t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (domainType is null)
                return Results.NotFound($"Typen '{type}' hittades inte.");

            if (!Guid.TryParse(id, out var guid))
                return Results.BadRequest($"Ogiltigt id-format: '{id}'.");

            var clrType = ResolveClrType(domainType.FullName);
            if (clrType is null)
                return Results.NotFound($"CLR-typ '{domainType.FullName}' kunde inte resolvas.");

            using var dbCtx = DbContextAccessor.GetReadContext(ctx.RequestServices);
            var entity = LoadEntityById(dbCtx, clrType, guid);

            if (entity is null)
                return Results.NotFound($"{type} med id '{id}' hittades inte.");

            return Results.Content(
                DataRelationPage.Render(domainType.Name, id, relation, entity, clrType, dbCtx),
                "text/html");
        }).ExcludeFromDescription();

        // --- POST routes ---

        // Statisk fabriksmetod: POST /data/{type}/{method}
        app.MapPost("/data/{type}/{method}", async (string type, string method, HttpContext ctx) =>
        {
            try
            {
                var model = SemanticRegistry.GetModel();
                var domainType = model.Types.FirstOrDefault(t =>
                    t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

                if (domainType is null)
                    return Results.NotFound($"Typen '{type}' hittades inte.");

                var clrType = ResolveClrType(domainType.FullName);
                if (clrType is null)
                    return Results.NotFound($"CLR-typ '{domainType.FullName}' kunde inte resolvas.");

                var methodInfo = FindMethod(clrType, method, isStatic: true);
                if (methodInfo is null)
                    return Results.NotFound($"Statisk metod '{method}' hittades inte på {type}.");

                var json = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
                var args = ParameterConverter.ConvertParameters(methodInfo.GetParameters(), json);

                var result = methodInfo.Invoke(null, args);

                // Om resultatet är ett aggregat/entitet — persistera
                await using var dbCtx = DbContextAccessor.GetWriteContext(ctx.RequestServices);
                dbCtx.Add(result!);
                await dbCtx.SaveChangesAsync();

                // Extrahera id för redirect
                var idProp = result!.GetType().GetProperty("Id");
                if (idProp is not null)
                {
                    var idVal = idProp.GetValue(result);
                    var guidProp = idVal?.GetType().GetProperty("Value");
                    if (guidProp is not null)
                    {
                        var guid = (Guid)guidProp.GetValue(idVal)!;
                        return Results.Json(new { id = guid.ToString(), type = domainType.Name, url = $"/data/{domainType.Name}/{guid}" });
                    }
                }

                return Results.Ok(new { status = "ok" });
            }
            catch (Exception ex)
            {
                return DomainMappingExtensions.HandleException(ex);
            }
        }).ExcludeFromDescription();

        // Instansmetod: POST /data/{type}/{id}/{method}
        app.MapPost("/data/{type}/{id}/{method}", async (string type, string id, string method, HttpContext ctx) =>
        {
            try
            {
                var model = SemanticRegistry.GetModel();
                var domainType = model.Types.FirstOrDefault(t =>
                    t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

                if (domainType is null)
                    return Results.NotFound($"Typen '{type}' hittades inte.");

                if (!Guid.TryParse(id, out var guid))
                    return Results.BadRequest($"Ogiltigt id-format: '{id}'.");

                var clrType = ResolveClrType(domainType.FullName);
                if (clrType is null)
                    return Results.NotFound($"CLR-typ '{domainType.FullName}' kunde inte resolvas.");

                await using var dbCtx = DbContextAccessor.GetWriteContext(ctx.RequestServices);
                var entity = LoadEntityById(dbCtx, clrType, guid);

                if (entity is null)
                    return Results.NotFound($"{type} med id '{id}' hittades inte.");

                var methodInfo = FindMethod(clrType, method, isStatic: false);
                if (methodInfo is null)
                    return Results.NotFound($"Metod '{method}' hittades inte på {type}.");

                var json = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
                var args = ParameterConverter.ConvertParameters(methodInfo.GetParameters(), json);

                var result = methodInfo.Invoke(entity, args);

                await dbCtx.SaveChangesAsync();

                return Results.Json(new { status = "ok", result = result?.ToString() });
            }
            catch (Exception ex)
            {
                return DomainMappingExtensions.HandleException(ex);
            }
        }).ExcludeFromDescription();
    }

    private static MethodInfo? FindMethod(Type clrType, string methodName, bool isStatic)
    {
        var binding = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        return clrType.GetMethods(binding)
            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase)
                                 && !m.IsSpecialName);
    }

    internal static Type? ResolveClrType(string fullName)
    {
        foreach (var asm in SemanticRegistry.GetDomainAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t is not null) return t;
        }
        return null;
    }

    internal static object? LoadEntityById(Microsoft.EntityFrameworkCore.DbContext dbCtx, Type clrType, Guid guid)
    {
        var idProp = clrType.GetProperty("Id");
        if (idProp is null) return null;

        var idValue = DbContextAccessor.ConstructId(idProp.PropertyType, guid);
        var query = DbContextAccessor.GetQueryable(dbCtx, clrType);
        query = DbContextAccessor.ApplyIncludes(query, clrType, dbCtx);
        query = DbContextAccessor.ApplyIdFilter(query, clrType, idValue);
        return DbContextAccessor.FirstOrDefault(query, clrType);
    }
}
