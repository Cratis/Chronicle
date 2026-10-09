// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSinkFactory.when_discovering;

/// <summary>
/// The kernel resolves <see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/> from the contract-to-implementor
/// map the type discovery generator emits for this assembly; this reads that map.
/// </summary>
public class and_the_kernel_types_are_scanned : Specification
{
    IEnumerable<Type> _factories;

    void Because()
    {
        var provider = typeof(EventSequenceSinkFactory).Assembly.GetTypes()
            .Single(_ => typeof(ICanProvideContractToImplementorsForDiscovery).IsAssignableFrom(_) && !_.IsAbstract);
        var map = ((ICanProvideContractToImplementorsForDiscovery)Activator.CreateInstance(provider)!).ContractsAndImplementors;
        _factories = map[typeof(ISinkFactory)];
    }

    [Fact] void should_discover_the_event_sequence_sink_factory() => _factories.ShouldContain(typeof(EventSequenceSinkFactory));
}
