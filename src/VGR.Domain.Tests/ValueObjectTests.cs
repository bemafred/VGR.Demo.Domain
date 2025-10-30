using System;
using VGR.Domain.SharedKernel;
using Xunit;
using FluentAssertions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain.Tests;

public class ValueObjectTests
{
    [Theory]
    [InlineData("19900101-1234", "199001011234")]
    [InlineData("19851212-0000", "198512120000")]
    [InlineData("199001011234",  "199001011234")]
    public void Personnummer_Parse_Normaliserar_Till_12Siffror(string input, string expected)
    {
        var p = Personnummer.Parse(input);
        ((string)p).Should().Be(expected);
    }

    [Theory]
    // Referensdatum 2025-01-01 ger:
    //  - '-' eller ingen separator: 90 -> 1990
    //  - '+'                      : 90 -> 1890
    [InlineData("900101-1234", "2025-01-01", "199001011234")]
    [InlineData("9001011234",  "2025-01-01", "199001011234")]
    [InlineData("900101+1234", "2025-01-01", "189001011234")]
    public void Personnummer_TryParse_MedReferens_Normaliserar_10Siffror(string input, string referenceDate, string expected)
    {
        var ok = Personnummer.TryParse(input, DateOnly.Parse(referenceDate), out var p);
        ok.Should().BeTrue();
        p.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void Personnummer_Parse_Fel_Kastar(string s)
    {
        Action act = () => Personnummer.Parse(s);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void HsaId_Parse_SätterTestFlagga()
    {
        var id = HsaId.Parse("HSA-T-123");
        id.IsTest.Should().BeTrue();
    }
}
