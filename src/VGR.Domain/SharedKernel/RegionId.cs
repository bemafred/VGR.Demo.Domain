namespace VGR.Domain.SharedKernel;

/// <summary>Starkt typat identitetsvärde för <see cref="Domain.Region"/>.</summary>
public readonly record struct RegionId(Guid Value)
{
    /// <summary>Genererar ett nytt unikt RegionId.</summary>
    public static RegionId Nytt() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}