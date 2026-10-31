// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing_to_all_events;

public class and_a_tail_request_was_deferred_until_ownership : given.a_new_observer_with_a_pending_tail_request
{
    Task Because() => _observer.SubscribeToAllEvents<ObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero);

    [Fact] void should_claim_the_definition_on_the_subscription_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_persist_the_event_after_the_tail() => _persistedStateStorage!.PersistedState.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(101UL);
    [Fact] void should_record_the_tail() => _persistedStateStorage!.PersistedState.TailEventSequenceNumber.ShouldEqual<EventSequenceNumber>(100UL);
    [Fact] void should_persist_the_identifier() => _persistedStateStorage!.PersistedState.Identifier.ShouldEqual(_observerId);
}
