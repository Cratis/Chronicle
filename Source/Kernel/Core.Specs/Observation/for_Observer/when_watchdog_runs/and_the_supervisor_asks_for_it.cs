// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// The supervisor runs the watchdog through the grain, so the duties happen without a timer of the observer's own.
/// </summary>
public class and_the_supervisor_asks_for_it : given.an_observer_with_client_owned_subscription
{
    void Establish() =>
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(false));

    async Task Because() => await _observer.CheckHealth();

    [Fact] async Task should_unsubscribe_the_observer_whose_client_is_gone() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
