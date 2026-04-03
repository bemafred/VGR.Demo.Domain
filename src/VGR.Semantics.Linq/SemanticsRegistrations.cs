using System;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace VGR.Semantics.Linq
{
    /// <summary>Fluent builder för att registrera semantiska expansioner manuellt (utöver automatisk <c>[ExpansionFor]</c>-upptäckt).</summary>
    public sealed class SemanticsRegistrationsBuilder
    {
        /// <summary>Registrerar en expansion för en domänmetod med två parametrar.</summary>
        public SemanticsRegistrationsBuilder Register<T1, T2, TResult>(
            Expression<Func<T1, T2, TResult>> domainCall,
            Expression<Func<T1, T2, TResult>> efExpression)
        {
            SemanticRegistry.Register(domainCall, efExpression);
            return this;
        }

        /// <summary>Registrerar en expansion för en domänmetod eller property med en parameter.</summary>
        public SemanticsRegistrationsBuilder Register<T1, TResult>(
            Expression<Func<T1, TResult>> domainCall,
            Expression<Func<T1, TResult>> efExpression)
        {
            SemanticRegistry.Register(domainCall, efExpression);
            return this;
        }
    }

    /// <summary>DI-extensions för att konfigurera semantiska frågeexpansioner.</summary>
    public static class SemanticsRegistrationExtensions
    {
        /// <summary>
        /// Valfritt anrop för manuella semantiska expansioner.
        /// <para>
        /// Automatisk upptäckt av <c>[ExpansionFor]</c>-metoder sker alltid via runtime reflection
        /// i <c>SemanticRegistry</c>:s statiska konstruktor — detta anrop behövs <b>inte</b> för
        /// att expansioner ska fungera.
        /// </para>
        /// <para>
        /// Använd detta enbart när en expansion inte kan uttryckas via <c>[ExpansionFor]</c>,
        /// t.ex. vid provider-specifika SQL-översättningar som kräver runtime-konfiguration.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// // Standardfallet — behövs inte:
        /// // [ExpansionFor]-attribut upptäcks automatiskt.
        ///
        /// // Specialfall — manuell registrering:
        /// services.AddQuerySemantics(b => b.Register&lt;Tidsrymd, DateTimeOffset, bool&gt;(
        ///     (r, t) => r.Innehåller(t),
        ///     (r, t) => r.Start &lt;= t &amp;&amp; (r.Slut == null || t &lt; r.Slut)));
        /// </code>
        /// </example>
        public static IServiceCollection AddQuerySemantics(this IServiceCollection services, Action<SemanticsRegistrationsBuilder>? configure = null)
        {
            var builder = new SemanticsRegistrationsBuilder();
            configure?.Invoke(builder);
            return services;
        }
    }
}