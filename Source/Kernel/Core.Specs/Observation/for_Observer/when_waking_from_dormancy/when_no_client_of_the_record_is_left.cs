// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

/// <summary>
/// Neither the client that held the subscription nor the standby it names is connected any more: the stale standby is skipped
/// and the observer ends up disconnected, with no record.
/// </summary>
public class when_no_client_of_the_record_is_left : given.a_dormant_client_observer_with_a_standby
{
    void Establish() => _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(false);

    async Task Because() => await Wake();

    [Fact] async Task should_not_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_deliver_anything() => _delivered.Count.ShouldEqual(0);
    [Fact] void should_drop_the_record() => _stateStorage.State.DurableSubscription.ShouldBeNull();
}
