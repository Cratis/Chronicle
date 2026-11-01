// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class and_a_webhook_takes_the_observer_while_restoring : given.a_reducer_grain_with_a_stored_definition
{
    Exception _exception;

    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _observer.RestoreClientSubscription<IReducerObserverSubscriber>(ObserverType.Reducer, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<ConnectedClient>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<EventGenerationDelivery>())
            .Returns(_ => throw new ObserverServedByAnotherTransport(new ObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log)));
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.KeepSubscription(_connectionDefinition));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] Task should_have_attempted_the_restore() => WasRestored(1);
    [Fact] async Task should_not_unsubscribe_the_webhook() => await _observer.DidNotReceive().Unsubscribe();
}
