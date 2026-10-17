// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_resubscribing_the_superseded_client_fails : given.an_observer_with_a_superseded_client
{
    void Establish()
    {
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns<Task<IEnumerable<EventTypeSchema>>>(_ => throw new Exception("Storage is unavailable"));
    }

    Task Because() => _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);

    [Fact] async Task should_be_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
