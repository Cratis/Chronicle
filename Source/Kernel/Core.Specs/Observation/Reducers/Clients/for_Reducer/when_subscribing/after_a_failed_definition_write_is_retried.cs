// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class after_a_failed_definition_write_is_retried : given.a_reducer_grain_with_a_stored_definition
{
    Exception _firstError;

    void Establish() => _stateStorage.WriteStateAsync().Returns(Task.FromException(new InvalidOperationException("storage unavailable")), Task.CompletedTask);

    async Task Because()
    {
        _firstError = await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_connectionDefinition));
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
    }

    [Fact] void should_fail_the_first_registration() => _firstError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] Task should_write_the_definition_again_on_retry() => _stateStorage.Received(2).WriteStateAsync();
    [Fact] void should_keep_the_new_definition() => _stateStorage.State.ShouldEqual(_connectionDefinition);
}
