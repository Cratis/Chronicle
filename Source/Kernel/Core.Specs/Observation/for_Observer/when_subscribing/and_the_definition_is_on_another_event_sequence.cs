// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_definition_is_on_another_event_sequence : given.an_observer_under_a_mismatching_key
{
    Exception? _replayError;

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _replayError = await Catch.Exception(() => _observer.ReplayPartition("some-partition"));
    }

    [Fact] void should_move_the_definition_to_the_event_sequence_of_the_key() => _definitionStorage.State.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_write_the_definition() => _definitionStorageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_write_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_accept_partition_operations_afterwards() => _replayError.ShouldBeNull();
}
