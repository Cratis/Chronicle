// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class with_a_changed_stored_definition : given.a_reactor_grain_with_a_stored_definition
{
    async Task Because() => await _grain.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] Task should_write_the_definition_once() => _stateStorage.Received(1).WriteStateAsync();
    [Fact] void should_keep_the_new_definition() => _stateStorage.State.ShouldEqual(_connectionDefinition);
    [Fact] Task should_subscribe_the_client() => WasSubscribed(_connectionDefinition);
}
