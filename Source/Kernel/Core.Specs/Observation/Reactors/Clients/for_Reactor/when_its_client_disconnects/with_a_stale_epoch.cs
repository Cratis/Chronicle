// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_its_client_disconnects;

public class with_a_stale_epoch : given.a_reactor_grain_with_replay_on_definition_change
{
    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _grain.ClientDisconnected(ClientConnectionEpoch.New());
    }

    [Fact] async Task should_not_unsubscribe_its_observer() => await _observerProbe.DidNotReceive().UnsubscribeIfMatchesClient(Arg.Any<ConnectionId>());
}
