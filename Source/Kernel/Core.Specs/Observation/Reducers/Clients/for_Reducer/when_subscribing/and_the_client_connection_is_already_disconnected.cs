// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class and_the_client_connection_is_already_disconnected : given.a_reducer_grain_with_replay_on_definition_change
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _clientConnection.Subscribe(Arg.Any<GrainId>()).Returns(ClientConnectionEpoch.NotSet);
        _definition = _definition with { IsActive = true };
    }

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_definition);

    [Fact] async Task should_unsubscribe_its_observer() => await _observer.Received(1).UnsubscribeIfMatchesClient("the-connection");
}
