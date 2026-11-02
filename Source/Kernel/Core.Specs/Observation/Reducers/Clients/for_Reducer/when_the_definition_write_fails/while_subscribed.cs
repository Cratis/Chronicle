// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_the_definition_write_fails;

public class while_subscribed : given.a_reducer_with_a_newer_owner
{
    async Task Establish()
    {
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _observer.ClearReceivedCalls();
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>()).Returns(ReducerDefinitionCompareResult.Different);
        _stateStorage.WriteStateAsync().Returns(Task.FromException(new InvalidOperationException()));
    }

    async Task Because() => await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_connectionDefinition with { ReadModel = "another-read-model" }));

    [Fact] async Task should_not_unsubscribe() => await _observer.DidNotReceive().Unsubscribe();
    [Fact] void should_remain_subscribed() => Subscribed.ShouldBeTrue();
}
