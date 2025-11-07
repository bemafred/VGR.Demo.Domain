namespace VGR.Domain.SharedKernel;

public readonly record struct VårdvalId(Guid Value)
{
    public static VårdvalId Nytt() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}