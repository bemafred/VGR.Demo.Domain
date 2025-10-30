namespace VGR.Domain.SharedKernel;

public readonly record struct VardvalId(Guid Value)
{
    public static VardvalId Nytt() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}