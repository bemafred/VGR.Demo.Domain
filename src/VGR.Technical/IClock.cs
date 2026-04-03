
namespace VGR.Technical;

/// <summary>Abstraktion för systemklockan. Möjliggör deterministisk tidshantering i tester.</summary>
public interface IClock
{
    /// <summary>Aktuell UTC-tidpunkt.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Systemklocka som returnerar verklig UTC-tid.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
