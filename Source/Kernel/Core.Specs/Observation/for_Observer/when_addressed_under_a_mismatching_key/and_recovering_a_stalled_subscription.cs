// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

public class and_recovering_a_stalled_subscription : given.an_observer_under_a_mismatching_key
{
    async Task Because() => await _observer.RecoverStalledSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_not_subscribe() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_keep_the_definition_on_its_event_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(_definitionEventSequenceId);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
