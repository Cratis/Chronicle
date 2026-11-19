// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_reading_the_same_definition_after_a_read_completed : given.the_provider
{
    async Task Because()
    {
        await _provider.ReadStateAsync("name", _grainId, _grainState);
        _definitionsStorage.Get(_observerId).Returns(DefinitionOn(_otherEventSequenceId));
        await _provider.ReadStateAsync("name", _grainId, _grainState);
    }

    [Fact] void should_read_storage_each_time() => _definitionsStorage.Received(2).Get(_observerId);
    [Fact] void should_pick_up_the_changed_definition() => _grainState.State.EventSequenceId.ShouldEqual(_otherEventSequenceId);
}
