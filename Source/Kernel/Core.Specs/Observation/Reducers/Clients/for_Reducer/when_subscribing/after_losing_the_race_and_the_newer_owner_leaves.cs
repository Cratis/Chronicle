// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class after_losing_the_race_and_the_newer_owner_leaves : given.a_reducer_with_a_newer_owner
{
    ReducerDefinition _definition;

    async Task Establish()
    {
        _definition = _connectionDefinition with { ReadModel = "another-read-model" };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>())
            .Returns(call => ReferenceEquals(call.ArgAt<ReducerDefinition>(1), call.ArgAt<ReducerDefinition>(2)) ? ReducerDefinitionCompareResult.Same : ReducerDefinitionCompareResult.Different);

        // The first registration loses the race to a newer client, the second one is accepted.
        SubscribeOn(_observer).Returns(
            Task.FromException(new ObserverOwnedByNewerClientVersion(_ownerKey, _ownerKey.EventSequenceId, "2.0.0", "1.0.0")),
            Task.CompletedTask);

        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
        _observer.ClearReceivedCalls();
        _recommendations.ClearReceivedCalls();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(31));
        await _grain.KeepSubscription(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_take_over() => Subscribed.ShouldBeTrue();
    [Fact] async Task should_replay_once() => await _observer.Received(1).Replay();
}
