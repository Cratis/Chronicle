// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_reading_a_missing_definition : given.the_provider
{
    Task Because() => _provider.ReadStateAsync("name", _grainId, _grainState);

    [Fact] void should_use_the_event_sequence_of_the_key() => _grainState.State.EventSequenceId.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_set_the_identifier() => _grainState.State.Identifier.ShouldEqual(_observerId);
}
