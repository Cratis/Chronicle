// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_a_dormant_observer;

public class and_asking_for_its_connected_clients : given_a_dormant_observer
{
    IEnumerable<Contracts.Clients.ConnectedClient> _clients;

    void Establish()
    {
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(true);
        connectedClients.GetConnectedClient(Arg.Any<ConnectionId>()).Returns(new Concepts.Clients.ConnectedClient { ConnectionId = "the-connection", MachineName = "the-machine" });
        _grains.GetGrain<IConnectedClients>(Arg.Any<string>(), Arg.Any<string>()).Returns(connectedClients);
    }

    async Task Because() => _clients = await _service.GetConnectedClientsForObserver(new GetConnectedClientsForObserverRequest
    {
        EventStore = "store",
        Namespace = "cratis",
        ObserverId = "observer",
        EventSequenceId = "event-log"
    });

    [Fact] void should_not_wake_the_observer() => _observer.DidNotReceive().GetSubscription();
    [Fact] void should_return_the_client_the_record_names() => _clients.Select(client => client.MachineName).ShouldContainOnly("the-machine");
}
