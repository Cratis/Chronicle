// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducersManager;

public class when_a_stored_reducer_targets_the_read_model : given.a_reducers_manager
{
    void Establish() => _eventStoreStorage.Reducers.HasForReadModel("read-model").Returns(true);

    async Task Because()
    {
        await _grain.HasReducerForReadModel("read-model");
        _result = await _grain.HasReducerForReadModel("read-model");
    }

    [Fact] void should_find_a_reducer_persisted_after_activation() => _result.ShouldBeTrue();
    [Fact] async Task should_use_a_filtered_query_for_each_read() => await _eventStoreStorage.Reducers.Received(2).HasForReadModel("read-model");
    [Fact] async Task should_not_scan_stored_definitions() => await _eventStoreStorage.Reducers.DidNotReceive().GetAll();
}
