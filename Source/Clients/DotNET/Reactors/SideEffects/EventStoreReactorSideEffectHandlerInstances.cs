// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Provides side-effect handlers to a cached event store without resolving the three event-type-registry handlers
/// from the root service provider.
/// </summary>
/// <param name="serviceProvider">The application service provider used for all other discovered handlers.</param>
/// <param name="logger">Logger for skipped discovered handlers.</param>
internal sealed class EventStoreReactorSideEffectHandlerInstances(IServiceProvider serviceProvider, ILogger<EventStoreReactorSideEffectHandlerInstances>? logger = null) : IInstancesOf<IReactorSideEffectHandler>
{
    static readonly ConcurrentDictionary<Type, byte> _reportedMissingHandlers = new();
    static readonly Type[] _eventTypeRegistryHandlerTypes =
    [
        typeof(EventResultHandler),
        typeof(EventsResultHandler),
        typeof(MixedSideEffectsResultHandler)
    ];

    readonly ILogger<EventStoreReactorSideEffectHandlerInstances> _logger = logger ?? NullLogger<EventStoreReactorSideEffectHandlerInstances>.Instance;
    readonly Type[] _otherHandlerTypes = TypeUniverse.For(serviceProvider)
        .FindMultiple<IReactorSideEffectHandler>()
        .Where(type => !_eventTypeRegistryHandlerTypes.Contains(type))
        .ToArray();

    /// <inheritdoc/>
    public IEnumerator<IReactorSideEffectHandler> GetEnumerator()
    {
        // These handlers receive the exact event store on the additive CanHandle overload. Constructing them here
        // avoids resolving their scoped compatibility registrations through ChronicleClient's root provider.
        yield return new EventResultHandler();
        yield return new EventsResultHandler();
        yield return new MixedSideEffectsResultHandler();

        var resolvedTypes = new HashSet<Type>(_eventTypeRegistryHandlerTypes);

        // Interface registrations are valid even when the concrete type is not registered separately.
        foreach (var handler in serviceProvider.GetServices<IReactorSideEffectHandler>())
        {
            if (resolvedTypes.Add(handler.GetType()))
            {
                yield return handler;
            }
        }

        foreach (var type in _otherHandlerTypes)
        {
            if (resolvedTypes.Contains(type)) continue;

            // DefaultServiceProvider activates via a parameterless constructor rather than using DI.
            // Do not attempt to activate an integration whose dependencies cannot be supplied.
            if (serviceProvider is DefaultServiceProvider && type.GetConstructor(Type.EmptyTypes) is null)
            {
                ReportMissingHandler(type);
                continue;
            }

            if (serviceProvider.GetService(type) is IReactorSideEffectHandler handler)
            {
                resolvedTypes.Add(type);
                yield return handler;
            }
            else
            {
                ReportMissingHandler(type);
            }
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ReportMissingHandler(Type type)
    {
        if (_logger.IsEnabled(LogLevel.Warning) && _reportedMissingHandlers.TryAdd(type, 0))
        {
            _logger.DiscoveredHandlerNotRegistered(type);
        }
    }
}
