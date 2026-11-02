// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class after_losing_the_race_and_the_newer_owner_leaves : given.a_reactor_with_a_newer_owner
{
    ReactorDefinition _definition;

    async Task Establish()
    {
        _definition = _connectionDefinition with { };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(call => ReferenceEquals(call.ArgAt<ReactorDefinition>(1), call.ArgAt<ReactorDefinition>(2)) ? ReactorDefinitionCompareResult.Same : ReactorDefinitionCompareResult.Different);

        // The first registration loses the race to a newer client, the second one is accepted.
        SubscribeOn(_observer).Returns(
            Task.FromException(new ObserverOwnedByNewerClientVersion(_ownerKey, _ownerKey.EventSequenceId, "2.0.0", "1.0.0")),
            Task.CompletedTask);

        await _grain.SetDefinitionAndSubscribe(_definition);
        _observer.ClearReceivedCalls();
        _recommendations.ClearReceivedCalls();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(31));
        await _grain.KeepSubscription(_definition);
    }

    [Fact] void should_take_over() => Subscribed.ShouldBeTrue();
    [Fact] async Task should_recommend_a_replay_once() => await ReplayRecommendedOn(_recommendations.Received(1));
}
