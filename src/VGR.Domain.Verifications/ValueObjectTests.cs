using System;
using VGR.Domain.SharedKernel;
using Xunit;
using FluentAssertions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.Verifications;

public class ValueObjectTests
{
    [Theory]
    [InlineData("19900101-1234", "199001011234")]
    [InlineData("19851212-0000", "198512120000")]
    [InlineData("199001011234",  "199001011234")]
    public void Personnummer_Tolka_Normaliserar_Till_12Siffror(string input, string expected)
    {
        var p = Personnummer.Tolka(input);
        ((string)p).Should().Be(expected);
    }

    [Theory]
    // Referensdatum 2025-01-01 ger:
    //  - '-' eller ingen separator: 90 -> 1990
    //  - '+'                      : 90 -> 1890
    [InlineData("900101-1234", "2025-01-01", "199001011234")]
    [InlineData("9001011234",  "2025-01-01", "199001011234")]
    [InlineData("900101+1234", "2025-01-01", "189001011234")]
    public void Personnummer_FörsökTolka_MedReferens_Normaliserar_10Siffror(string input, string referenceDate, string expected)
    {
        var ok = Personnummer.FörsökTolka(input, DateOnly.Parse(referenceDate), out var p);
        ok.Should().BeTrue();
        p.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void Personnummer_Tolka_Fel_Kastar(string s)
    {
        Action act = () => Personnummer.Tolka(s);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void HsaId_Parse_SätterTestFlagga()
    {
        var id = HsaId.Tolka("HSA-T-123");
        id.IsTest.Should().BeTrue();
    }
}
