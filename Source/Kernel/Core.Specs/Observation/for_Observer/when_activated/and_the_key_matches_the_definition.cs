// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_the_key_matches_the_definition : given.an_observer
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { EventSequenceId = EventSequenceId.Log };
        await Crash();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _storageStats.ResetCounts();
    }

    async Task Because() => await _observer.OnDeactivateAsync(default, default);

    [Fact] void should_set_running_state_to_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_keep_the_definition_on_its_event_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(EventSequenceId.Log);
}
