// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_the_definition_write_fails;

public class for_the_first_registration : given.a_reducer_grain_with_a_stored_definition
{
    Exception _exception;

    void Establish()
    {
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>()).Returns(ReducerDefinitionCompareResult.New);
        _stateStorage.WriteStateAsync().Returns(Task.FromException(new InvalidOperationException()));
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_connectionDefinition));

    [Fact] void should_fail_the_registration() => _exception.ShouldNotBeNull();
    [Fact] async Task should_not_subscribe_the_observer() => await SubscribeOn(_observer.DidNotReceive());
}
