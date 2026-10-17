// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_the_superseded_client_disconnected_first : given.an_observer_with_a_superseded_client
{
    async Task Establish()
    {
        // Still reported as connected, so only the dropped registration can keep the observer from handing over to it.
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        await _observer.UnsubscribeIfMatchesClient(_supersededClient.ConnectionId);
    }

    Task Because() => _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);

    [Fact] async Task should_be_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
