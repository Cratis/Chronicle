// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorMethodArgumentsResolver"/>.
/// </summary>
[Singleton]
public class ReactorMethodArgumentsResolver : IReactorMethodArgumentsResolver, IRegisteredReactorMethodArgumentsResolver
{
    /// <inheritdoc/>
    public Task<object?[]> Resolve(
        MethodInfo method,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider) =>
        Resolve(method, reactor, @event, eventContext, eventStore, serviceProvider, null);

    /// <inheritdoc/>
    Task<object?[]> IRegisteredReactorMethodArgumentsResolver.Resolve(
        MethodInfo method,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider,
        ReactorDelivery delivery) =>
        Resolve(method, reactor, @event, eventContext, eventStore, serviceProvider, delivery);

    static async Task<object?[]> Resolve(
        MethodInfo method,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider,
        ReactorDelivery? delivery)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        if (parameters.Length == 0)
        {
            return arguments;
        }

        arguments[0] = @event;

        for (var index = 1; index < parameters.Length; index++)
        {
            arguments[index] = await ResolveArgument(
                parameters[index].ParameterType,
                reactor,
                @event,
                eventContext,
                eventStore,
                serviceProvider,
                delivery);
        }

        return arguments;
    }

    static async Task<object?> ResolveArgument(
        Type parameterType,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider,
        ReactorDelivery? delivery)
    {
        if (parameterType == typeof(EventContext))
        {
            return eventContext;
        }

        if (parameterType == typeof(ReactorDelivery))
        {
            return delivery ?? ResolveDelivery(reactor, eventContext, eventStore);
        }

        if (eventStore is not null && IsReadModel(eventStore, parameterType))
        {
            var key = reactor is ICanResolveReadModelKey keyResolver
                ? keyResolver.Resolve(@event, eventContext)
                : (ReadModelKey)eventContext.EventSourceId;
            return await eventStore.ReadModels.GetInstanceById(parameterType, key);
        }

        if (serviceProvider is null)
        {
            throw new CannotResolveReactorMethodArgument(parameterType);
        }

        return serviceProvider.GetRequiredService(parameterType);
    }

    static ReactorDelivery ResolveDelivery(object reactor, EventContext eventContext, IEventStore? eventStore)
    {
        if (eventStore is null)
        {
            return ReactorDelivery.For(reactor, eventContext);
        }

        var reactorType = reactor.GetType();
        if (eventStore.Reactors is Reactors reactors && reactors.Handlers.FirstOrDefault(_ => _.ReactorType == reactorType) is { } handler)
        {
            return ReactorDelivery.For(handler.Id, handler.EventSequenceId, eventContext);
        }

        // Preserve standalone declaration-based resolution when the store has no native registration for this type.
        var sequence = reactorType.GetEventSequenceId(eventStore.EventTypes, eventStore.Name);
        return ReactorDelivery.For(reactorType.GetReactorId(), sequence, eventContext);
    }

    static bool IsReadModel(IEventStore eventStore, Type type) =>
        eventStore.Projections.HasFor(type) || eventStore.Reducers.HasFor(type);
}
