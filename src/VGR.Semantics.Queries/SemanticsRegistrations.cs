using System;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace VGR.Semantics.Queries
{
    // Fluent builder that forwards registrations to the internal SemanticRegistry.
    public sealed class SemanticsRegistrationsBuilder
    {
        public SemanticsRegistrationsBuilder Register<T1, T2, TResult>(
            Expression<Func<T1, T2, TResult>> domainCall,
            Expression<Func<T1, T2, TResult>> efExpression)
        {
            SemanticRegistry.Register(domainCall, efExpression);
            return this;
        }

        public SemanticsRegistrationsBuilder Register<T1, TResult>(
            Expression<Func<T1, TResult>> domainCall,
            Expression<Func<T1, TResult>> efExpression)
        {
            SemanticRegistry.Register(domainCall, efExpression);
            return this;
        }
    }

    public static class SemanticsRegistrationExtensions
    {
        // Keeps the IServiceCollection-centric pattern common in ASP.NET Core.
        public static IServiceCollection AddQuerySemantics(this IServiceCollection services, Action<SemanticsRegistrationsBuilder>? configure = null)
        {
            var builder = new SemanticsRegistrationsBuilder();
            configure?.Invoke(builder);
            return services;
        }
    }
}