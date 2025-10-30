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

public sealed class DomainArgumentFormatException(string code, string message) : DomainException(code, message);

/// <summary>
/// Samlad och IntelliSense-vänlig fabrik för domänundantag.
/// Används för att kasta väl namngivna fel nära regeln som bryts.
/// <code>
/// if (slut < start) Throw.Vårdval.EndBeforeStart(start, slut);
/// if (!Personnummer.TryCreate(raw, out _)) Throw.Person.InvalidPersonnummer(raw);
/// </code>
/// </summary>
[StackTraceHidden]
public static class Throw
{
    /// <summary>
    /// Tillhandahåller domänspecifika undantag relaterade till användaråtgärder.
    /// </summary>
    /// <remarks>
    /// Denna klass är en del av domänens undantagshanteringsmekanism och används för att kasta väldefinierade undantag
    /// vid obehöriga användaråtgärder inom applikationslagret.
    /// </remarks>
    public static class Användare
    {
        /// <summmary>Ej auktoriserad användare.</summmary>
        /// <remarks>För applikationslagret.</remarks>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void EjAuktoriserad(string meddelande)
            => throw new UnauthorizedAccessException(meddelande);
    }

    public static class Region
    {
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

    /// <summary>
    /// Tillhandahåller metoder för att kasta undantag relaterade till personspecifik validering och tillståndsövergångar.
    /// </summary>
    public static class Person
    {
        /// <summary>Ogiltigt personnummer (format/semantik).</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void OgiltigtPersonnummer(string raw)
            => throw new DomainValidationException(
                $"{nameof(Person)}.{nameof(OgiltigtPersonnummer)}",
                $"Ogiltigt personnummer: '{raw}'.");

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Dubblett(string personnummmer)
            => throw new DomainInvariantViolationException(
                $"{nameof(Person)}.{nameof(Dubblett)}",
                $"Personnummer '{personnummmer}' finns redan.");

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Saknas(PersonId regionId)
            => throw new DomainAggregateNotFoundException(
                $"{nameof(Person)}.{nameof(Saknas)}",
                $"Person med Id {regionId} kan inte hittas."
            );

        /// <summary>Försök att avsluta vårdval när inget aktivt finns.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void IngetAktivtVårdvalAttStänga()
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Person)}.{nameof(IngetAktivtVårdvalAttStänga)}",
                "Det finns inget aktivt vårdval att avsluta.");

        /// <summary>
        /// VårdvalSaknas
        /// </summary>
        /// <param name="meddelande"></param>
        /// <exception cref="DomainInvalidStateTransitionException"></exception>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void VårdvalSaknas(string meddelande)
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Person)}.{nameof(VårdvalSaknas)}",
                meddelande);

        /// <summary>
        /// Person hittades inte
        /// </summary>
        /// <param name="meddelande"></param>
        /// <exception cref="DomainInvalidStateTransitionException"></exception>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void HittadesInte(string meddelande)
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Person)}.{nameof(HittadesInte)}",
                meddelande);
    }

    /// <summary>
    /// 
    /// </summary>
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
        public static void AlreadyActiveExists()
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.RedanAktivt",
                "Det finns redan ett aktivt vårdval och ytterligare får inte skapas.");

        /// <summary>Försök att avsluta ett redan avslutat vårdval.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void RedanAvslutat()
            => throw new DomainInvalidStateTransitionException(
                $"{nameof(Vårdval)}.{nameof(RedanAvslutat)}",
                "Vårdvalet är redan avslutat.");

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void VårdvalGällerIntePåHsaId(string hsaId, string meddelande)
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(VårdvalGällerIntePåHsaId)}",
                $"Vårdvalet gäller inte på HSA-ID {hsaId} {meddelande}.");

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Överlapp(SharedKernel.HsaId enhetHsaId, Tidsrymd giltighet)
            => throw new DomainInvariantViolationException(
                $"{nameof(Vårdval)}.{nameof(Överlapp)}",
                $"Överlappande vårdval på HSA-ID {enhetHsaId} {giltighet}.");
    }

    public static class Vårdcentral
    {
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

    public static class Concurrency // TODO: Även tekniska undantag kanske ska hanteras av domän metoder?
    {
        /// <summary>Optimistisk samtidighetskonflikt vid uppdatering.</summary>
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Conflict(long expected, long actual)
            => throw new DomainConcurrencyConflictException(
                $"{nameof(Concurrency)}.{nameof(Conflict)}", // TODO: Ingen bra kod här? Hur?
                $"Samtidighetskonflikt: förväntad version {expected}, aktuell {actual}."
                );
    }

    public static class Idempotency // TODO: Även tekniska undantag kanske ska hanteras av domän metoder?
    {
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Duplicate(string key)
            => throw new DomainIdempotencyViolationException(
                $"{nameof(Idempotency)}.{nameof(Duplicate)}", // TODO: Ingen bra kod här? Hur?
                $"Idempotens nyckel '{key}' har redan använts."
                );
    }

    private static string Format(DateTimeOffset? dt) => dt is null ? "–" : ((DateTimeOffset)dt).ToString("O");
    private static string Format(DateTimeOffset dt) => dt.ToString("O");
}

