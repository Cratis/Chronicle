// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_subscriber_returns_disconnected_while_a_superseded_client_is_connected : given.an_observer_with_a_superseded_client
{
    ObserverSubscription _subscription;
    IClientOwnedObserverSubscriber _clientSubscriber;

    void Establish()
    {
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        _clientSubscriber = Substitute.For<IClientOwnedObserverSubscriber>();
        _clientSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Disconnected()));
        _silo.AddProbe(_ => _clientSubscriber);
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 42UL,
            LastHandledEventSequenceNumber = 41UL
        };
    }

    async Task Because()
    {
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(active_event_type, 42UL)]);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_be_subscribed() => _subscription.IsSubscribed.ShouldBeTrue();
    [Fact] void should_use_the_event_types_of_the_superseded_client() => _subscription.EventTypes.ShouldContainOnly(superseded_event_type);
    [Fact] void should_target_the_superseded_client() => _subscription.Targets.Single().ConnectedClient!.ConnectionId.ShouldEqual(_supersededClient.ConnectionId);
    [Fact] void should_not_advance_the_next_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_advance_the_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)41UL);
}
