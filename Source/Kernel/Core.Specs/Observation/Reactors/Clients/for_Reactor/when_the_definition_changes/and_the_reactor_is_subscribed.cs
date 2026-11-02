// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_the_definition_changes;

public class and_the_reactor_is_subscribed : given.a_reactor_with_a_newer_owner
{
    async Task Establish()
    {
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _stateStorage.ClearReceivedCalls();
        _observer.ClearReceivedCalls();
        _recommendations.ClearReceivedCalls();
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>()).Returns(ReactorDefinitionCompareResult.Different);
    }

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_connectionDefinition with { });

    [Fact] void should_write_the_definition_before_resubscribing() => Received.InOrder(() =>
    {
        _stateStorage.WriteStateAsync();
        _observer.Unsubscribe();
        SubscribeOn(_observer);
    });

    [Fact] void should_recommend_the_replay_after_subscribing() => Received.InOrder(() =>
    {
        SubscribeOn(_observer);
        ReplayRecommendedOn(_recommendations);
    });
}
