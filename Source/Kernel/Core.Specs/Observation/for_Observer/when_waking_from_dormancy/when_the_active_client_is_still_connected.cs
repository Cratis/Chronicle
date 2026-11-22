// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

/// <summary>
/// The client that held the subscription is still connected: it keeps it, and the standby the record names stays a standby.
/// </summary>
public class when_the_active_client_is_still_connected : given.a_dormant_client_observer_with_a_standby
{
    ObserverSubscription _subscription;

    async Task Because()
    {
        await Wake();
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_target_the_active_client() => _subscription.Targets.Select(target => target.ConnectedClient!.ConnectionId).ShouldContainOnly(_activeClient.ConnectionId);
    [Fact] async Task should_know_the_standby() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeTrue();
    [Fact] async Task should_not_have_the_standby_as_a_target() => (await _observer.IsClientTarget(_client.ConnectionId)).ShouldBeFalse();
}
