// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducersManager;

public class when_a_registered_reducer_targets_the_read_model : given.a_reducers_manager
{
    void Establish() => _state.Reducers = [_definition with { EventSequenceId = "system" }];

    async Task Because() => _result = await _grain.HasReducerForReadModel("read-model");

    [Fact] void should_find_the_reducer() => _result.ShouldBeTrue();
    [Fact] async Task should_not_query_stored_targets() => await _eventStoreStorage.Reducers.DidNotReceive().HasForReadModel("read-model");
    [Fact] async Task should_not_scan_stored_definitions() => await _eventStoreStorage.Reducers.DidNotReceive().GetAll();
}
