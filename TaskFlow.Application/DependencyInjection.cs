using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Behaviors;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Scoped);

        RegisterCommandHandlers(services, assembly);
        services.RegisterHandlers(assembly, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegisterCommandHandlers(
        IServiceCollection services,
        Assembly assembly)
    {
        var handlerInterface = typeof(ICommandHandler<,>);

        foreach (var (impl, iface) in assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (impl: t, iface: i))))
        {
            // Register concrete handler under a keyed name so the decorator can resolve it
            services.AddKeyedScoped(iface, "inner", impl);

            // Register the decorator as the primary resolution of the interface
            var typeArgs = iface.GetGenericArguments();
            var decoratorType = typeof(ValidationBehavior<,>).MakeGenericType(typeArgs);

            services.AddScoped(iface, sp =>
            {
                var inner = sp.GetRequiredKeyedService(iface, "inner");
                return ActivatorUtilities.CreateInstance(sp, decoratorType, inner);
            });
        }
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
