namespace VGR.Domain.SharedKernel;

public readonly record struct RegionId(Guid Value)
{
    public static RegionId Nytt() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}