namespace VGR.Domain.SharedKernel.Exceptions;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VGR.Domain.SharedKernel;

public abstract class DomainException(string code, string message) : Exception(message)
{
    /// <summary>Stabil, maskinläsbar felkod (ex: "Person.SlutFöreStart").</summary>
    public string Code { get; } = code;

    /// <summary>Markerar tekniska/transienta fel som kan vara rimliga att försöka om.</summary>
    public virtual bool IsTransient => false;
    public override string ToString() => $"{GetType().Name}({Code}): {Message}";
}

/// <summary>En invarians/regel har brutits (detta ska aldrig vara giltigt tillstånd).</summary>
public sealed class DomainInvariantViolationException(string code, string message) : DomainException(code, message);

/// <summary>En otillåten tillståndsövergång har försökts (fel ordning i processen).</summary>
public sealed class DomainInvalidStateTransitionException(string code, string message) : DomainException(code, message);

/// <summary>Semantisk validering av ett domänvärde misslyckades (format/innehåll).</summary>
public sealed class DomainValidationException(string code, string message) : DomainException(code, message);

/// <summary>Optimistisk samtidighetskonflikt (någon annan hann spara först).</summary>
public sealed class DomainConcurrencyConflictException(string code, string message) : DomainException(code, message);

/// <summary>Efterfrågat aggregat/objekt finns inte (när flödet förutsätter att det finns).</summary>
public sealed class DomainAggregateNotFoundException(string code, string message) : DomainException(code, message);

/// <summary>Idempotens nyckel återanvänd (samma kommando har redan körts).</summary>
public sealed class DomainIdempotencyViolationException(string code, string message) : DomainException(code, message);

/// <summary>Externt argument har ogiltigt format (rå indata → domänvärde misslyckas).</summary>
public sealed class DomainArgumentFormatException(string code, string message) : DomainException(code, message);

/// <summary>Operationen saknar semantisk mening för det aktuella värdet eller tillståndet.</summary>
public sealed class DomainUndefinedOperationException(string code, string message) : DomainException(code, message);

/// <summary>
/// Samlad och IntelliSense-vänlig fabrik för domänundantag.
/// Används för att kasta väl namngivna fel nära regeln som bryts.
/// <code>
/// if (slut &lt; start) Throw.Vårdval.SlutFöreStart(start, slut);
/// if (!Personnummer.FörsökTolka(raw, out _)) Throw.Personnummer.OgiltigtPersonnummer(raw);
/// </code>
/// </summary>
[StackTraceHidden]
public static class Throw
{
    public static class Region
    {
        /// <summary>Region med angivet id saknas.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Saknas(RegionId regionId)
            => throw new DomainAggregateNotFoundException(
                $"{nameof(Region)}.{nameof(Saknas)}",
                $"Region med Id {regionId} kan inte hittas"  
                );
    }

    public static class Personnummer
    {
        /// <summary>Ogiltigt personnummer (format/semantik).</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void OgiltigtPersonnummer(string raw)
            => throw new DomainValidationException(
                $"{nameof(Personnummer)}.{nameof(OgiltigtPersonnummer)}",
                $"Ogiltigt personnummer: '{raw}'.");
    }

    public static class Person
    {
        /// <summary>Personnummer finns redan inom regionen.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Dubblett(string personnummmer)
            => throw new DomainInvariantViolationException(
                $"{nameof(Person)}.{nameof(Dubblett)}",
                $"Personnummer '{personnummmer}' finns redan.");

        /// <summary>Person med angivet id saknas.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Saknas(PersonId personId)
            => throw new DomainAggregateNotFoundException(
                $"{nameof(Person)}.{nameof(Saknas)}",
                $"Person med Id {personId} kan inte hittas."
            );

        /// <summary>Försök att avsluta vårdval när inget aktivt finns.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void IngetAktivtVårdvalAttStänga()
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Person)}.{nameof(IngetAktivtVårdvalAttStänga)}",
                "Det finns inget aktivt vårdval att avsluta.");

    }

    public static class Vårdval
    {
        /// <summary>Slutdatum före startdatum.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void SlutFöreStart(DateTimeOffset start, DateTimeOffset? slut)
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(SlutFöreStart)}",
                $"Slut {Format(slut)} före start {Format(start)} är ogiltigt.");

        /// <summary>Försök skapa ett nytt vårdval fast ett aktivt redan finns.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void AktivtFinnsRedan()
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(AktivtFinnsRedan)}",
                "Det finns redan ett aktivt vårdval och ytterligare får inte skapas.");

        /// <summary>Försök att avsluta ett redan avslutat vårdval.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void RedanAvslutat()
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Vårdval)}.{nameof(RedanAvslutat)}",
                "Vårdvalet är redan avslutat.");

        /// <summary>Vårdvalet gäller inte på angivet HSA-ID.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void VårdvalGällerIntePåHsaId(string hsaId, string meddelande)
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(VårdvalGällerIntePåHsaId)}",
                $"Vårdvalet gäller inte på HSA-ID {hsaId} {meddelande}.");

        /// <summary>Nytt vårdvals startdatum ligger före det aktiva vårdvalets start.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void StartFöreAktivtVårdval(DateTimeOffset aktivtStart, DateTimeOffset nyttStart)
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(StartFöreAktivtVårdval)}",
                $"Nytt vårdval med start {Format(nyttStart)} kan inte starta före det aktiva vårdvalets start {Format(aktivtStart)}.");

        /// <summary>Överlappande vårdval på samma enhet är inte tillåtet.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void ÖverlappEjTillåtet(SharedKernel.HsaId enhetHsaId, SharedKernel.Tidsrymd giltighet)
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(ÖverlappEjTillåtet)}",
                $"Överlappande vårdval på HSA-ID {enhetHsaId} {giltighet}.");
    }

    public static class Vårdcentral
    {
        /// <summary>Läkaren är inte valbar på angiven enhet.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void LäkareEjValbarPåEnhet(string hsaId, string läkarHsaId)
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Vårdcentral)}.{nameof(LäkareEjValbarPåEnhet)}",
                $"Läkare med HSA-ID {läkarHsaId} är inte valbar på enhet med HSA-ID {hsaId}.");
    }

    public static class HsaId
    {
        /// <summary>Ogiltigt HSA-ID (format/semantik).</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Ogiltigt(string raw)
            => throw new DomainValidationException(
                $"{nameof(HsaId)}.{nameof(Ogiltigt)}",
                $"Ogiltigt HSA-ID: '{raw}'.");

        /// <summary>Ingen mappning hittades för HSA-ID → internt id saknas.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void MappningSaknas(string hsaId)
            => throw new DomainAggregateNotFoundException(
                $"{nameof(HsaId)}.{nameof(MappningSaknas)}",
                $"HSA-ID '{hsaId}' saknar mappning till internt id.");
    }

    public static class Concurrency
    {
        /// <summary>Optimistisk samtidighetskonflikt vid uppdatering.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Conflict(long expected, long actual)
            => throw new DomainConcurrencyConflictException(
                $"{nameof(Concurrency)}.{nameof(Conflict)}",
                $"Samtidighetskonflikt: förväntad version {expected}, aktuell {actual}.");
    }

    public static class Idempotency
    {
        /// <summary>Idempotens nyckel redan använd.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Duplicate(string key)
            => throw new DomainIdempotencyViolationException(
                $"{nameof(Idempotency)}.{nameof(Duplicate)}",
                $"Idempotens nyckel '{key}' har redan använts.");
    }

    public static class Tidsrymd
    {
        /// <summary>Slut före start.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void SlutFöreStart(DateTimeOffset start, DateTimeOffset? slut)
            => throw new DomainValidationException(
                $"{nameof(Tidsrymd)}.{nameof(SlutFöreStart)}",
                $"Slut {Format(slut)} måste vara ≥ start {Format(start)}.");

        /// <summary>Steg måste vara positivt.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void StegMåsteVaraPositivt(TimeSpan steg)
            => throw new DomainValidationException(
                $"{nameof(Tidsrymd)}.{nameof(StegMåsteVaraPositivt)}",
                $"Steg måste vara positivt, var {steg}.");

        /// <summary>Varaktighet är odefinierad för tillsvidare-intervall.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void VaraktighetOdefinieradFörTillsvidare()
            => throw new DomainUndefinedOperationException(
                $"{nameof(Tidsrymd)}.{nameof(VaraktighetOdefinieradFörTillsvidare)}",
                "Varaktighet är odefinierad för tillsvidare-intervall.");

        /// <summary>Stegning kräver avgränsat intervall.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void StegningKräverAvgränsatIntervall()
            => throw new DomainUndefinedOperationException(
                $"{nameof(Tidsrymd)}.{nameof(StegningKräverAvgränsatIntervall)}",
                "Kan inte stega ett tillsvidare-intervall.");

        /// <summary>Enumerering kräver avgränsat intervall.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void EnumereringKräverAvgränsatIntervall()
            => throw new DomainUndefinedOperationException(
                $"{nameof(Tidsrymd)}.{nameof(EnumereringKräverAvgränsatIntervall)}",
                "Kan inte enumerera ett tillsvidare-intervall.");

        /// <summary>Omfång kräver minst ett intervall.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void OmfångKräverMinstEttIntervall()
            => throw new DomainUndefinedOperationException(
                $"{nameof(Tidsrymd)}.{nameof(OmfångKräverMinstEttIntervall)}",
                "Tom sekvens — omfång kräver minst ett intervall.");

        /// <summary>Konvertering till Datumintervall kräver avgränsat intervall.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void DatumintervallKräverAvgränsatIntervall()
            => throw new DomainUndefinedOperationException(
                $"{nameof(Tidsrymd)}.{nameof(DatumintervallKräverAvgränsatIntervall)}",
                "Kan inte skapa Datumintervall från ett tillsvidare-intervall.");
    }

    public static class Datumintervall
    {
        /// <summary>Slut före start.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void SlutFöreStart(DateOnly start, DateOnly? slut)
            => throw new DomainValidationException(
                $"{nameof(Datumintervall)}.{nameof(SlutFöreStart)}",
                $"Slut {slut} måste vara ≥ start {start}.");

        /// <summary>Enumerering kräver avgränsat intervall.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void EnumereringKräverAvgränsatIntervall()
            => throw new DomainUndefinedOperationException(
                $"{nameof(Datumintervall)}.{nameof(EnumereringKräverAvgränsatIntervall)}",
                "Kan inte avge ett tillsvidare-intervall.");
    }

    private static string Format(DateTimeOffset? dt) => dt is null ? "–" : ((DateTimeOffset)dt).ToString("O");
    private static string Format(DateTimeOffset dt) => dt.ToString("O");
}

