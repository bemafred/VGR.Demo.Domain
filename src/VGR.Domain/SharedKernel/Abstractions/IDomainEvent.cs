namespace VGR.Domain.SharedKernel.Abstractions;

/// <summary>Kontrakt för en domänhändelse med unikt id och tidpunkt.</summary>
public interface IDomainEvent
{
    /// <summary>Unikt händelse-id.</summary>
    Guid EventId { get; }
    /// <summary>Tidpunkt då händelsen inträffade.</summary>
    DateTimeOffset OccurredAt { get; }
}

/// <summary>Basklass för domänhändelser med automatgenererat EventId.</summary>
public abstract record DomainEvent(DateTimeOffset OccurredAt) : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();
}