// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_timed_out_original_client_is_superseded : given.an_observer_with_a_superseded_client
{
    static readonly SiloAddress _supersededSiloAddress = SiloAddress.FromParsableString("127.0.0.1:11111@1");
    IClientOwnedObserverSubscriber _client;
    IEnumerable<DeliveryFence> _fences;
    ObserverSubscription _current;

    void Establish()
    {
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        _client = Substitute.For<IClientOwnedObserverSubscriber>();
        _silo.AddProbe(_ => _client);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 42UL, LastHandledEventSequenceNumber = 41UL };
    }

    async Task Because()
    {
        // A remains connected after B replaces it because their event types differ.
        var key = _observerKey;
        var original = new DeliveryFenceTarget(typeof(IClientOwnedObserverSubscriber).AssemblyQualifiedName!, new ObserverSubscriberKey(key.ObserverId, key.EventStore, key.Namespace, key.EventSequenceId, "partition", _supersededSiloAddress.ToParsableString()).ToString(), _supersededClient.ConnectionId, _supersededSiloAddress.ToParsableString());
        await _observer.PartitionFailed("partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout, deliveryFence: new(DateTimeOffset.UtcNow.AddSeconds(300), [original]));
        await _observer.ClearFailedPartitions();
        await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(active_event_type, 42UL)]);
        _current = await _observer.GetSubscription();
        _fences = await _observer.GetDeliveryFences();
    }

    [Fact] void should_keep_the_original_connected_A_identity() => _fences.Single().Targets.Single().ConnectionId.ShouldEqual(_supersededClient.ConnectionId.ToString());
    [Fact] void should_still_have_B_as_the_active_subscription() => _current.Targets.Single().ConnectedClient!.ConnectionId.ShouldEqual(_activeClient.ConnectionId);
    [Fact] void should_not_dispatch_to_B_after_clearance() => _ = _client.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_advance_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)41UL);
}
