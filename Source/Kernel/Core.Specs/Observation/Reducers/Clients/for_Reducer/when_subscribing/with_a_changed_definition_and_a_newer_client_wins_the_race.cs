// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class with_a_changed_definition_and_a_newer_client_wins_the_race : given.a_reducer_with_a_newer_owner
{
    ReducerDefinition _definition;

    void Establish()
    {
        _definition = _connectionDefinition with { ReadModel = "another-read-model" };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>()).Returns(ReducerDefinitionCompareResult.Different);
        SubscribeOn(_observer).Returns(Task.FromException(new ObserverOwnedByNewerClientVersion(_ownerKey, _ownerKey.EventSequenceId, "2.0.0", "1.0.0")));
    }

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_remain_in_standby() => Subscribed.ShouldBeFalse();
    [Fact] void should_warn_once() => ShouldWarnOnce();
    [Fact] async Task should_not_replay() => await _observer.DidNotReceive().Replay();
}
