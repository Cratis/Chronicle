// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_its_client_disconnects;

public class with_a_stale_epoch : given.a_reducer_grain_with_replay_on_definition_change
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _definition = _definition with { IsActive = true };
    }

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _grain.ClientDisconnected(ClientConnectionEpoch.New());
    }

    [Fact] async Task should_not_unsubscribe_its_observer() => await _observer.DidNotReceive().UnsubscribeIfMatchesClient(Arg.Any<ConnectionId>());
}
