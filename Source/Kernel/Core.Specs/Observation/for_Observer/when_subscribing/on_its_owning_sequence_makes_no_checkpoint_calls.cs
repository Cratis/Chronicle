// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class on_its_owning_sequence_makes_no_checkpoint_calls : given.a_live_observer_with_guarded_state
{
    async Task Because() => await _observer.SubscribeToAllEvents<NullObserverSubscriber>(Concepts.Observation.ObserverType.Projection, SiloAddress.Zero);

    [Fact] void should_not_touch_the_checkpoints() => _eventStoreNamespaceStorage.ObserverSequenceCheckpoints.ReceivedCalls().ShouldBeEmpty();
}
