// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Provides side-effect handlers to a cached event store without resolving the three event-type-registry handlers
/// from the root service provider.
/// </summary>
/// <param name="serviceProvider">The application service provider used for all other discovered handlers.</param>
internal sealed class EventStoreReactorSideEffectHandlerInstances(IServiceProvider serviceProvider) : IInstancesOf<IReactorSideEffectHandler>
{
    static readonly Type[] _eventTypeRegistryHandlerTypes =
    [
        typeof(EventResultHandler),
        typeof(EventsResultHandler),
        typeof(MixedSideEffectsResultHandler)
    ];

    readonly Type[] _otherHandlerTypes = Types.Types.Instance
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

        foreach (var type in _otherHandlerTypes)
        {
            yield return (IReactorSideEffectHandler)serviceProvider.GetRequiredService(type);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
