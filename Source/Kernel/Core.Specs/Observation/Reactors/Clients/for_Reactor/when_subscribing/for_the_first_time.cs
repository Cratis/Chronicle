// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class for_the_first_time : given.a_reactor_grain_with_a_stored_definition
{
    void Establish() => _definitionComparer.Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>()).Returns(ReactorDefinitionCompareResult.New);

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] void should_write_the_definition_before_subscribing() => Received.InOrder(() =>
    {
        _stateStorage.WriteStateAsync();
        SubscribeOn(_observer);
    });
}
