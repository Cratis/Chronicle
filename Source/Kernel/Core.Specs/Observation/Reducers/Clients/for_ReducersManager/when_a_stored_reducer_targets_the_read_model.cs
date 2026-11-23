// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducersManager;

public class when_a_stored_reducer_targets_the_read_model : given.a_reducers_manager
{
    void Establish() => _eventStoreStorage.Reducers.GetAll().Returns([_definition with { EventSequenceId = "system" }]);

    async Task Because() => _result = await _grain.HasReducerForReadModel("read-model");

    [Fact] void should_find_a_reducer_persisted_after_activation() => _result.ShouldBeTrue();
}
