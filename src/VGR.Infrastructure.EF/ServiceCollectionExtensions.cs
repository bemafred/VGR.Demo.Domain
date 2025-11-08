using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using VGR.Infrastructure.EF.Translators;

namespace VGR.Infrastructure.EF;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTidsrymdEfTranslators(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMemberTranslatorPlugin, TidsrymdTranslatorsPlugin>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMethodCallTranslatorPlugin, TidsrymdTranslatorsPlugin>());
        return services;
    }
}
