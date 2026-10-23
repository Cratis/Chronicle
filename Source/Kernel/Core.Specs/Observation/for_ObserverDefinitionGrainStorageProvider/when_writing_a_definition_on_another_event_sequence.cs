// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_writing_a_definition_on_another_event_sequence : given.the_provider
{
    Exception? _error;

    void Establish() => _grainState.State = DefinitionOn(_otherEventSequenceId);

    async Task Because() => _error = await Catch.Exception(() => _provider.WriteStateAsync("name", _grainId, _grainState));

    [Fact] void should_refuse_the_write() => _error.ShouldBeOfExactType<ObserverKeyDoesNotMatchDefinition>();
    [Fact] async Task should_not_save_the_definition() => await _definitionsStorage.DidNotReceive().Save(Arg.Any<ObserverDefinition>());
}
