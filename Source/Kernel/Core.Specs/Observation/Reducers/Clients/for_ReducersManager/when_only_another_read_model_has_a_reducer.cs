// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducersManager;

public class when_only_another_read_model_has_a_reducer : given.a_reducers_manager
{
    void Establish() => _eventStoreStorage.Reducers.GetAll().Returns([_definition with { ReadModel = "another-read-model" }]);

    async Task Because() => _result = await _grain.HasReducerForReadModel("read-model");

    [Fact] void should_not_find_a_reducer() => _result.ShouldBeFalse();
}
