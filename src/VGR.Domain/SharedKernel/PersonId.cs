namespace VGR.Domain.SharedKernel;

/// <summary>Starkt typat identitetsvärde för <see cref="Domain.Person"/>.</summary>
public readonly record struct PersonId(Guid Value)
{
    /// <summary>Genererar ett nytt unikt PersonId.</summary>
    public static PersonId Nytt() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}