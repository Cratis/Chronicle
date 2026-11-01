// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class and_a_webhook_wins_the_subscription_race : given.a_reducer_grain_with_a_stored_definition
{
    Exception _exception;

    void Establish() => _observer.Subscribe<IReducerObserverSubscriber>(ObserverType.Reducer, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>())
        .Returns(_ => throw new ObserverServedByAnotherTransport(new ObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log)));

    async Task Because() => _exception = await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_connectionDefinition));

    [Fact] void should_not_fail_the_stream_registration() => _exception.ShouldBeNull();
    [Fact] async Task should_not_unsubscribe_the_webhook() => await _observer.DidNotReceive().Unsubscribe();
}
