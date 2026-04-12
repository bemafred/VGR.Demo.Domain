using VGR.Technical;

namespace VGR.Technical.Testing;

/// <summary>Deterministisk klocka för tester. Fast tidpunkt 2024-01-01T12:00:00Z.</summary>
public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; } = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
}
