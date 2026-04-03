using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;
using Xunit;
using FluentAssertions;

namespace VGR.Domain.Verifications;

public class ThrowVerifications
{
    [Fact]
    public void Region_Saknas_Kastar_AggregateNotFound()
    {
        var act = () => Throw.Region.Saknas(new RegionId(Guid.NewGuid()));

        act.Should().Throw<DomainAggregateNotFoundException>()
           .Which.Code.Should().Be("Region.Saknas");
    }

    [Fact]
    public void Person_Saknas_Kastar_AggregateNotFound()
    {
        var act = () => Throw.Person.Saknas(new PersonId(Guid.NewGuid()));

        act.Should().Throw<DomainAggregateNotFoundException>()
           .Which.Code.Should().Be("Person.Saknas");
    }

    [Fact]
    public void Person_IngetAktivtVårdvalAttStänga_Kastar_InvalidStateTransition()
    {
        var act = () => Throw.Person.IngetAktivtVårdvalAttStänga();

        act.Should().Throw<DomainInvalidStateTransitionException>()
           .Which.Code.Should().Be("Person.IngetAktivtVårdvalAttStänga");
    }

    [Fact]
    public void Vårdval_AktivtFinnsRedan_Kastar_InvariantViolation()
    {
        var act = () => Throw.Vårdval.AktivtFinnsRedan();

        act.Should().Throw<DomainInvariantViolationException>()
           .Which.Code.Should().Be("Vårdval.AktivtFinnsRedan");
    }

    [Fact]
    public void Vårdval_RedanAvslutat_Kastar_InvalidStateTransition()
    {
        var act = () => Throw.Vårdval.RedanAvslutat();

        act.Should().Throw<DomainInvalidStateTransitionException>()
           .Which.Code.Should().Be("Vårdval.RedanAvslutat");
    }

    [Fact]
    public void Vårdval_VårdvalGällerIntePåHsaId_Kastar_InvariantViolation()
    {
        var act = () => Throw.Vårdval.VårdvalGällerIntePåHsaId("HSA-123", "test");

        act.Should().Throw<DomainInvariantViolationException>()
           .Which.Code.Should().Be("Vårdval.VårdvalGällerIntePåHsaId");
    }

    [Fact]
    public void Vårdval_StartFöreAktivtVårdval_Kastar_InvariantViolation()
    {
        var aktivtStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var nyttStart = new DateTimeOffset(2023, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var act = () => Throw.Vårdval.StartFöreAktivtVårdval(aktivtStart, nyttStart);

        act.Should().Throw<DomainInvariantViolationException>()
           .Which.Code.Should().Be("Vårdval.StartFöreAktivtVårdval");
    }

    [Fact]
    public void Vårdcentral_LäkareEjValbarPåEnhet_Kastar_InvalidStateTransition()
    {
        var act = () => Throw.Vårdcentral.LäkareEjValbarPåEnhet("HSA-ENHET-1", "HSA-LÄKARE-1");

        act.Should().Throw<DomainInvalidStateTransitionException>()
           .Which.Code.Should().Be("Vårdcentral.LäkareEjValbarPåEnhet");
    }

    [Fact]
    public void HsaId_MappningSaknas_Kastar_AggregateNotFound()
    {
        var act = () => Throw.HsaId.MappningSaknas("HSA-OKÄND");

        act.Should().Throw<DomainAggregateNotFoundException>()
           .Which.Code.Should().Be("HsaId.MappningSaknas");
    }

    [Fact]
    public void Concurrency_Conflict_Kastar_ConcurrencyConflict()
    {
        var act = () => Throw.Concurrency.Conflict(expected: 1, actual: 2);

        act.Should().Throw<DomainConcurrencyConflictException>()
           .Which.Code.Should().Be("Concurrency.Conflict");
    }

    [Fact]
    public void Idempotency_Duplicate_Kastar_IdempotencyViolation()
    {
        var act = () => Throw.Idempotency.Duplicate("nyckel-abc");

        act.Should().Throw<DomainIdempotencyViolationException>()
           .Which.Code.Should().Be("Idempotency.Duplicate");
    }

    // --- Tidsrymd-fabriker (ADR-008) ---

    [Fact]
    public void Tidsrymd_SlutFöreStart_Kastar_Validation()
    {
        var start = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var slut = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var act = () => Throw.Tidsrymd.SlutFöreStart(start, slut);

        act.Should().Throw<DomainValidationException>()
           .Which.Code.Should().Be("Tidsrymd.SlutFöreStart");
    }

    [Fact]
    public void Tidsrymd_StegMåsteVaraPositivt_Kastar_Validation()
    {
        var act = () => Throw.Tidsrymd.StegMåsteVaraPositivt(TimeSpan.Zero);

        act.Should().Throw<DomainValidationException>()
           .Which.Code.Should().Be("Tidsrymd.StegMåsteVaraPositivt");
    }

    [Fact]
    public void Tidsrymd_VaraktighetOdefinieradFörTillsvidare_Kastar_UndefinedOperation()
    {
        var act = () => Throw.Tidsrymd.VaraktighetOdefinieradFörTillsvidare();

        act.Should().Throw<DomainUndefinedOperationException>()
           .Which.Code.Should().Be("Tidsrymd.VaraktighetOdefinieradFörTillsvidare");
    }

    [Fact]
    public void Tidsrymd_StegningKräverAvgränsatIntervall_Kastar_UndefinedOperation()
    {
        var act = () => Throw.Tidsrymd.StegningKräverAvgränsatIntervall();

        act.Should().Throw<DomainUndefinedOperationException>()
           .Which.Code.Should().Be("Tidsrymd.StegningKräverAvgränsatIntervall");
    }

    [Fact]
    public void Tidsrymd_EnumereringKräverAvgränsatIntervall_Kastar_UndefinedOperation()
    {
        var act = () => Throw.Tidsrymd.EnumereringKräverAvgränsatIntervall();

        act.Should().Throw<DomainUndefinedOperationException>()
           .Which.Code.Should().Be("Tidsrymd.EnumereringKräverAvgränsatIntervall");
    }

    [Fact]
    public void Tidsrymd_OmfångKräverMinstEttIntervall_Kastar_UndefinedOperation()
    {
        var act = () => Throw.Tidsrymd.OmfångKräverMinstEttIntervall();

        act.Should().Throw<DomainUndefinedOperationException>()
           .Which.Code.Should().Be("Tidsrymd.OmfångKräverMinstEttIntervall");
    }

    [Fact]
    public void Tidsrymd_DatumintervallKräverAvgränsatIntervall_Kastar_UndefinedOperation()
    {
        var act = () => Throw.Tidsrymd.DatumintervallKräverAvgränsatIntervall();

        act.Should().Throw<DomainUndefinedOperationException>()
           .Which.Code.Should().Be("Tidsrymd.DatumintervallKräverAvgränsatIntervall");
    }

    // --- Datumintervall-fabriker (ADR-008) ---

    [Fact]
    public void Datumintervall_SlutFöreStart_Kastar_Validation()
    {
        var act = () => Throw.Datumintervall.SlutFöreStart(new DateOnly(2024, 6, 1), new DateOnly(2024, 1, 1));

        act.Should().Throw<DomainValidationException>()
           .Which.Code.Should().Be("Datumintervall.SlutFöreStart");
    }

    [Fact]
    public void Datumintervall_EnumereringKräverAvgränsatIntervall_Kastar_UndefinedOperation()
    {
        var act = () => Throw.Datumintervall.EnumereringKräverAvgränsatIntervall();

        act.Should().Throw<DomainUndefinedOperationException>()
           .Which.Code.Should().Be("Datumintervall.EnumereringKräverAvgränsatIntervall");
    }
}
