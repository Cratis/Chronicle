// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class after_a_newer_client_claims_another_sequence : given.a_reducer_with_a_newer_owner
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(true);

    async Task Because()
    {
        await _grain.KeepSubscription(_connectionDefinition);
        await _grain.KeepSubscription(_connectionDefinition);
    }

    [Fact] void should_wait_in_standby() => Subscribed.ShouldBeFalse();
    [Fact] async Task should_leave_the_observer_on_its_own_sequence() => await _observer.Received(1).UnsubscribeIfMatchesClient(_client.ConnectionId);
    [Fact] async Task should_not_write_the_definition() => await _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_subscribe_the_observer() => await _observer.DidNotReceiveWithAnyArgs().Subscribe<IReducerObserverSubscriber>(ObserverType.Reducer, [], SiloAddress.Zero);
    [Fact] void should_warn_once() => ShouldWarnOnce();
    [Fact] async Task should_recommend_once_with_the_conflict() => await _recommendations.Received(1).Add<IObserverRegistrationConflictRecommendation, ObserverRegistrationConflictRequest>(
        Arg.Any<RecommendationDescription>(),
        Arg.Is<ObserverRegistrationConflictRequest>(request => request.OwnerEventSequenceId == _ownerKey.EventSequenceId && request.IncomingVersion == "1.0.0" && request.OwnerVersion == "2.0.0" && request.Outcome == "standby"));
}
