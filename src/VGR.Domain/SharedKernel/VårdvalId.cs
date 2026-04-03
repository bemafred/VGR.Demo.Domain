namespace VGR.Domain.SharedKernel;

/// <summary>Starkt typat identitetsvärde för <see cref="Domain.Vårdval"/>.</summary>
public readonly record struct VårdvalId(Guid Value)
{
    /// <summary>Genererar ett nytt unikt VårdvalId.</summary>
    public static VårdvalId Nytt() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}