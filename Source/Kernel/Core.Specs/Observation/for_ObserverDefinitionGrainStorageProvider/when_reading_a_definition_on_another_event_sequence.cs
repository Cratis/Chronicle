// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_reading_a_definition_on_another_event_sequence : given.the_provider
{
    void Establish() => _definitionsStorage.Get(_observerId).Returns(DefinitionOn(_otherEventSequenceId));

    Task Because() => _provider.ReadStateAsync("name", _grainId, _grainState);

    [Fact] void should_keep_the_stored_event_sequence() => _grainState.State.EventSequenceId.ShouldEqual(_otherEventSequenceId);
    [Fact] void should_set_the_identifier() => _grainState.State.Identifier.ShouldEqual(_observerId);
}
