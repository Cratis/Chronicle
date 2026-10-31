// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_its_client_disconnects;

public class with_the_current_epoch : given.a_reactor_grain_with_replay_on_definition_change
{
    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _grain.ClientDisconnected(_epoch);
    }

    [Fact] async Task should_unsubscribe_its_observer() => await _observerProbe.Received(1).UnsubscribeIfMatchesClient("the-connection");
}
