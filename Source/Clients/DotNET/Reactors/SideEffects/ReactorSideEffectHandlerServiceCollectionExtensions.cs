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
    /// Registers the built-in handlers and dispatcher once per scope. Their published singleton metadata is kept
    /// for compatibility, while these explicit registrations ensure the previous event-store-less contract uses
    /// the exact <see cref="Cratis.Chronicle.Events.IEventTypes"/> registry selected for the current scope.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the handlers to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddReactorSideEffectHandlers(this IServiceCollection services)
    {
        services.TryAddScoped<EventResultHandler>(serviceProvider =>
            new(serviceProvider.GetRequiredService<Cratis.Chronicle.Events.IEventTypes>()));
        services.TryAddScoped<EventsResultHandler>(serviceProvider =>
            new(serviceProvider.GetRequiredService<Cratis.Chronicle.Events.IEventTypes>()));
        services.TryAddScoped<MixedSideEffectsResultHandler>(serviceProvider =>
            new(serviceProvider.GetRequiredService<Cratis.Chronicle.Events.IEventTypes>()));
        services.TryAddScoped<ReactorSideEffectHandlers>();
        services.TryAddScoped<IReactorSideEffectHandlers>(serviceProvider =>
            serviceProvider.GetRequiredService<ReactorSideEffectHandlers>());

        return services;
    }
}
