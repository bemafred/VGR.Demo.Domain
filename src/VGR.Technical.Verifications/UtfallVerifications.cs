using FluentAssertions;
using VGR.Technical;

namespace VGR.Technical.Verifications;

public class UtfallVerifications
{
    [Fact]
    public void Ok_ÄrLyckad()
    {
        var utfall = Utfall.Ok();

        utfall.IsSuccess.Should().BeTrue();
        utfall.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_BärFelmeddelande()
    {
        var utfall = Utfall.Fail("något gick fel");

        utfall.IsSuccess.Should().BeFalse();
        utfall.Error.Should().Be("något gick fel");
    }

    [Fact]
    public void Ok_MedVärde_BärVärde()
    {
        var utfall = Utfall<int>.Ok(42);

        utfall.IsSuccess.Should().BeTrue();
        utfall.Value.Should().Be(42);
        utfall.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_MedVärdetyp_GerDefault()
    {
        var utfall = Utfall<int>.Fail("saknas");

        utfall.IsSuccess.Should().BeFalse();
        utfall.Value.Should().Be(default);
        utfall.Error.Should().Be("saknas");
    }

    [Fact]
    public void Fail_MedReferenstyp_GerNull()
    {
        var utfall = Utfall<string>.Fail("fel");

        utfall.IsSuccess.Should().BeFalse();
        utfall.Value.Should().BeNull();
        utfall.Error.Should().Be("fel");
    }

    [Fact]
    public void Fail_MedKod_BärKod()
    {
        var utfall = Utfall.Fail("redan registrerad", "Person.Dubblett");

        utfall.IsSuccess.Should().BeFalse();
        utfall.Code.Should().Be("Person.Dubblett");
        utfall.Error.Should().Be("redan registrerad");
    }

    [Fact]
    public void Fail_UtanKod_GerNullKod()
    {
        var utfall = Utfall.Fail("generellt fel");

        utfall.Code.Should().BeNull();
    }

    [Fact]
    public void FailT_MedKod_BärKod()
    {
        var utfall = Utfall<int>.Fail("saknas", "Person.Saknas");

        utfall.IsSuccess.Should().BeFalse();
        utfall.Code.Should().Be("Person.Saknas");
    }

    [Fact]
    public void Ok_HarIngenKod()
    {
        var utfall = Utfall.Ok();
        utfall.Code.Should().BeNull();

        var utfallT = Utfall<int>.Ok(1);
        utfallT.Code.Should().BeNull();
    }
}

public class ClockVerifications
{
    [Fact]
    public void SystemClock_UtcNow_ÄrNäraNuvarandeTid()
    {
        var clock = new SystemClock();

        var före = DateTimeOffset.UtcNow;
        var tid = clock.UtcNow;
        var efter = DateTimeOffset.UtcNow;

        tid.Should().BeOnOrAfter(före).And.BeOnOrBefore(efter);
    }
}
