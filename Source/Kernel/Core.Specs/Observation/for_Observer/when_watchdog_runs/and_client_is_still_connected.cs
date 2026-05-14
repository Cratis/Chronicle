// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_client_is_still_connected : given.an_observer_with_client_owned_subscription
{
    void Establish() =>
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_remain_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
