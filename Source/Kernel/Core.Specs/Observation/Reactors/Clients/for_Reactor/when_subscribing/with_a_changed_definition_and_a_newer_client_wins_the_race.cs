// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class with_a_changed_definition_and_a_newer_client_wins_the_race : given.a_reactor_with_a_newer_owner
{
    ReactorDefinition _definition;

    void Establish()
    {
        _definition = _connectionDefinition with { };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>()).Returns(ReactorDefinitionCompareResult.Different);
        SubscribeOn(_observer).Returns(Task.FromException(new ObserverOwnedByNewerClientVersion(_ownerKey, _ownerKey.EventSequenceId, "2.0.0", "1.0.0")));
    }

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
    }

    [Fact] void should_remain_in_standby() => Subscribed.ShouldBeFalse();
    [Fact] void should_warn_once() => ShouldWarnOnce();
    [Fact] async Task should_not_recommend_a_replay() => await ReplayRecommendedOn(_recommendations.DidNotReceive());
}
