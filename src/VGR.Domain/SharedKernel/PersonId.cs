namespace VGR.Domain.SharedKernel;

public readonly record struct PersonId(Guid Value)
{
    public static PersonId Nytt() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}