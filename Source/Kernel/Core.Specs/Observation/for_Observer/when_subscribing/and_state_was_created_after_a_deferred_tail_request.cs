// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_state_was_created_after_a_deferred_tail_request : given.a_new_observer_with_a_pending_tail_request
{
    void Establish() =>
        _persistedStateStorage!.PersistedState = _persistedStateStorage.PersistedState with { Identifier = _observerId, NextEventSequenceNumber = 5UL };

    Task Because() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);

    [Fact] void should_preserve_the_existing_position() => _persistedStateStorage!.PersistedState.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(5UL);
}
