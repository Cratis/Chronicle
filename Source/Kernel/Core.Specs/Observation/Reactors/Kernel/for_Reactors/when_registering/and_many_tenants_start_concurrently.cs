// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_many_tenants_start_concurrently : given.registrations
{
    int _activeWrites;
    int _maximumWrites;

    void Establish()
    {
        _definitions.Save(Arg.Any<ReactorDefinition>()).Returns(async call =>
        {
            var active = Interlocked.Increment(ref _activeWrites);
            _maximumWrites = Math.Max(_maximumWrites, active);

            // Widen the concurrent-startup interleaving; do not wait on wall-clock timing.
            await Task.Yield();
            var definition = call.Arg<ReactorDefinition>();
            _persisted[definition.Identifier] = definition;
            Interlocked.Decrement(ref _activeWrites);
        });
    }

    Task Because() => Task.WhenAll(Enumerable.Range(0, 40).Select(tenant =>
        _reactors.DiscoverAndRegister(_eventStore, (EventStoreNamespaceName)$"tenant-{tenant}")));

    [Fact] async Task should_write_event_store_metadata_only_once() => await _definitions.Received(1).Save(Arg.Any<ReactorDefinition>());
    [Fact] void should_admit_only_one_definition_write_at_a_time() => _maximumWrites.ShouldEqual(1);
    [Fact] void should_keep_the_required_event_type() => _persisted.Values.Single().EventTypes.ShouldNotBeEmpty();
    [Fact] void should_subscribe_every_tenant() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.Subscribe)).ShouldEqual(40);
}
