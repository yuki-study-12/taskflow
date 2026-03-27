using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.RegisterHandlers(assembly, typeof(ICommandHandler<,>));
        services.RegisterHandlers(assembly, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegisterHandlers(
        this IServiceCollection services,
        Assembly assembly,
        Type handlerType)
    {
        foreach (var (impl, iface) in assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == handlerType)
                .Select(i => (impl: t, iface: i))))
        {
            services.AddScoped(iface, impl);
        }
    }
}
