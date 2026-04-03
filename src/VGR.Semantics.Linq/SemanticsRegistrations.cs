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
        /// <summary>Registrerar semantisk frågeinfrastruktur i DI-containern. Automatisk <c>[ExpansionFor]</c>-upptäckt sker alltid; valfri <paramref name="configure"/> för manuella registreringar.</summary>
        public static IServiceCollection AddQuerySemantics(this IServiceCollection services, Action<SemanticsRegistrationsBuilder>? configure = null)
        {
            var builder = new SemanticsRegistrationsBuilder();
            configure?.Invoke(builder);
            return services;
        }
    }
}