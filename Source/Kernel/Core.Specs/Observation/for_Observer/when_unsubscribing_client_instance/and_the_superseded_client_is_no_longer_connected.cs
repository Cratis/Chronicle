// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_the_superseded_client_is_no_longer_connected : given.an_observer_with_a_superseded_client
{
    void Establish() => _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(false);

    Task Because() => _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);

    [Fact] async Task should_be_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
