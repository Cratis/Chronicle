// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class with_an_identical_stored_definition : given.a_reactor_grain_with_a_stored_definition
{
    void Establish() => _stateStorage.State = _connectionDefinition with { };

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] Task should_not_write_the_definition() => _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] Task should_subscribe_the_client() => WasSubscribed(_connectionDefinition);
}
