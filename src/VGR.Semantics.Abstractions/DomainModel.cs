using System;
using System.Collections.Generic;

namespace VGR.Semantics.Abstractions;

/// <summary>Domänens fullständiga statiska struktur, extraherad via reflection.</summary>
public sealed record DomainModel(IReadOnlyList<DomainType> Types);

/// <summary>En typ i domänen med klassificering, egenskaper och beteenden.</summary>
public sealed record DomainType(
    string Name,
    string FullName,
    DomainTypeKind Kind,
    IReadOnlyList<DomainProperty> Properties,
    IReadOnlyList<DomainMethod> Methods);

/// <summary>En publik egenskap på en domäntyp.</summary>
public sealed record DomainProperty(
    string Name,
    string TypeName,
    bool IsReadOnly,
    bool HasSemanticQuery);

/// <summary>En publik metod på en domäntyp.</summary>
public sealed record DomainMethod(
    string Name,
    IReadOnlyList<DomainParameter> Parameters,
    string ReturnType,
    bool IsStatic,
    bool HasSemanticQuery);

/// <summary>En parameter till en domänmetod.</summary>
public sealed record DomainParameter(string Name, string TypeName);

/// <summary>Klassificering av domäntyper baserad på strukturella heuristiker.</summary>
public enum DomainTypeKind
{
    Aggregate,
    Entity,
    Identity,
    ValueObject,
    DomainEvent,
    Exception,
    Static
}
