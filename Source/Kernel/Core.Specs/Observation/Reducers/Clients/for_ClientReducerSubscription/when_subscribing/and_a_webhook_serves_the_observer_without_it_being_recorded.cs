// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_subscribing;

public class and_a_webhook_serves_the_observer_without_it_being_recorded : given.a_client_reducer_subscription_with_a_stored_definition
{
    void Establish() => _observer.Subscribe<IReducerObserverSubscriber>(ObserverType.Reducer, Arg.Any<EventTypeSet>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>())
        .Returns(_ => throw new ObserverServedByAnotherTransport(new ObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log)));

    Task Because() => _subscription.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] Task should_not_record_the_observer() => _connectedClients.DidNotReceiveWithAnyArgs().RecordObserver(default!, default!);
}
