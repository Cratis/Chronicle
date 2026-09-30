// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering reactor side-effect handling with the event-store scope selected by the
/// current dependency-injection scope.
/// </summary>
internal static class ReactorSideEffectHandlerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the built-in handlers, reactor metadata providers and dispatcher. The registry-dependent handlers
    /// resolve once per scope so the previous event-store-less contract uses the exact
    /// <see cref="Cratis.Chronicle.Events.IEventTypes"/> registry selected for the current scope.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the handlers to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddReactorSideEffectHandlers(this IServiceCollection services)
    {
        // The registry-dependent handlers and the dispatcher are authoritative: an earlier registration, for example
        // by convention binding of the published singleton metadata, would otherwise win and capture the scoped registry.
        services.RemoveAll<EventResultHandler>();
        services.RemoveAll<EventsResultHandler>();
        services.RemoveAll<MixedSideEffectsResultHandler>();
        services.RemoveAll<ReactorSideEffectHandlers>();
        services.RemoveAll<IReactorSideEffectHandlers>();
        services.AddScoped<EventResultHandler>(serviceProvider =>
            new(serviceProvider.GetRequiredService<Cratis.Chronicle.Events.IEventTypes>()));
        services.AddScoped<EventsResultHandler>(serviceProvider =>
            new(serviceProvider.GetRequiredService<Cratis.Chronicle.Events.IEventTypes>()));
        services.AddScoped<MixedSideEffectsResultHandler>(serviceProvider =>
            new(serviceProvider.GetRequiredService<Cratis.Chronicle.Events.IEventTypes>()));
        foreach (var type in typeof(ReactorSideEffectHandlers).Assembly.GetTypes()
                     .Where(type => type.IsClass && !type.IsAbstract && typeof(IReactorContextValuesProvider).IsAssignableFrom(type)))
        {
            services.TryAddTransient(type);
        }

        foreach (var type in typeof(ReactorSideEffectHandlers).Assembly.GetTypes()
                     .Where(type => type.IsClass && !type.IsAbstract && typeof(IReactorSideEffectHandler).IsAssignableFrom(type))
                     .Except([typeof(EventResultHandler), typeof(EventsResultHandler), typeof(MixedSideEffectsResultHandler)]))
        {
            services.TryAddSingleton(type);
        }
        services.AddScoped<ReactorSideEffectHandlers>();
        services.AddScoped<IReactorSideEffectHandlers>(serviceProvider =>
            serviceProvider.GetRequiredService<ReactorSideEffectHandlers>());

        return services;
    }
}
