// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_the_definition_write_fails;

public class and_the_client_retries : given.a_reactor_with_a_newer_owner
{
    ReactorDefinition _definition;

    void Establish()
    {
        _definition = _connectionDefinition with { };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(call => ReferenceEquals(call.ArgAt<ReactorDefinition>(1), call.ArgAt<ReactorDefinition>(2)) ? ReactorDefinitionCompareResult.Same : ReactorDefinitionCompareResult.Different);
        _stateStorage.WriteStateAsync().Returns(Task.FromException(new InvalidOperationException()), Task.CompletedTask);
    }

    async Task Because()
    {
        await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_definition));
        await _grain.SetDefinitionAndSubscribe(_definition);
    }

    [Fact] async Task should_subscribe_once() => await SubscribeOn(_observer.Received(1));
    [Fact] async Task should_recommend_a_replay_once() => await ReplayRecommendedOn(_recommendations.Received(1));
}
