// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_writing_a_definition_on_the_event_sequence_of_the_key : given.the_provider
{
    void Establish() => _grainState.State = DefinitionOn(_observerKey.EventSequenceId);

    Task Because() => _provider.WriteStateAsync("name", _grainId, _grainState);

    [Fact] async Task should_announce_the_change_to_every_silo() => await _cacheClient.Received(1).Invalidate(_observerKey.EventStore, _observerId);
    [Fact] async Task should_save_the_definition() => await _definitionsStorage.Received(1).Save(_grainState.State);
}
