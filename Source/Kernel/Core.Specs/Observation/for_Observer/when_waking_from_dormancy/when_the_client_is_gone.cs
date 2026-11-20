// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

/// <summary>
/// The record names a client that has left: it is advice that is no longer true, so it is dropped rather than delivered to.
/// </summary>
public class when_the_client_is_gone : given.a_dormant_client_observer
{
    void Establish()
    {
        Append(EventAt(0));
        _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(false);
    }

    async Task Because() => await Wake();

    [Fact] void should_not_deliver_anything() => _delivered.Count.ShouldEqual(0);
    [Fact] async Task should_not_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_drop_the_record() => _stateStorage.State.DurableSubscription.ShouldBeNull();
    [Fact] void should_forget_the_entry_the_queue_kept() => _appendedEventsQueues.Received().Forget(Arg.Any<ObserverKey>());
}
