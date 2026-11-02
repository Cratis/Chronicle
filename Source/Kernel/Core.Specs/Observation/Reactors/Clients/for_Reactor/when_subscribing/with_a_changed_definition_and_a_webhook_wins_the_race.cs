// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class with_a_changed_definition_and_a_webhook_wins_the_race : given.a_reactor_with_a_newer_owner
{
    ReactorDefinition _definition;

    void Establish()
    {
        _definition = _connectionDefinition with { };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>()).Returns(ReactorDefinitionCompareResult.Different);
        SubscribeOn(_observer).Returns(_ => throw new ObserverServedByAnotherTransport(new ObserverKey("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log)));
    }

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
    }

    [Fact] async Task should_write_the_definition() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] async Task should_still_recommend_a_replay() => await ReplayRecommendedOn(_recommendations.Received(1));
}
