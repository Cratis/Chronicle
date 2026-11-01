// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class after_an_older_client_wins_a_concurrent_registration : given.a_reactor_with_a_newer_owner
{
    void Establish()
    {
        _owner.GetSubscription().Returns(new ObserverSubscription(_connectionDefinition.Identifier, _ownerKey, [], typeof(IReactorObserverSubscriber), SiloAddress.Zero)
        {
            Targets = [new(SiloAddress.Zero, _client with { ConnectionId = "the-older-connection", Version = "0.9.0" })]
        });
        _observer.KnowsClient(_client.ConnectionId).Returns(true);
    }

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] void should_claim_the_observer_back() => Subscribed.ShouldBeTrue();
    [Fact] Task should_subscribe_the_observer() => WasSubscribed(_connectionDefinition);
    [Fact] async Task should_not_leave_the_observer() => await _observer.DidNotReceiveWithAnyArgs().UnsubscribeIfMatchesClient(default!);
    [Fact] async Task should_not_report_a_conflict() => await _recommendations.DidNotReceiveWithAnyArgs().Add<IObserverRegistrationConflictRecommendation, ObserverRegistrationConflictRequest>(default!, default!);
}
