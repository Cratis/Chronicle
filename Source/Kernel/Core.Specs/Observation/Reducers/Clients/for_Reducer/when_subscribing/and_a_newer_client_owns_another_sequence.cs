// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class and_a_newer_client_owns_another_sequence : given.a_reducer_with_a_newer_owner
{
    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        await _grain.KeepSubscription(_connectionDefinition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_wait_in_standby() => Subscribed.ShouldBeFalse();
    [Fact] async Task should_not_write_the_definition() => await _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] void should_not_change_the_stored_definition() => _stateStorage.State.ShouldEqual(_storedDefinition);
    [Fact] async Task should_not_subscribe_the_observer() => await _observer.DidNotReceiveWithAnyArgs().Subscribe<IReducerObserverSubscriber>(ObserverType.Reducer, [], SiloAddress.Zero);
    [Fact] async Task should_not_replay() => await _observer.DidNotReceive().Replay();
    [Fact] void should_warn_once() => ShouldWarnOnce();
    [Fact] async Task should_recommend_once_with_the_conflict() => await _recommendations.Received(1).Add<IObserverRegistrationConflictRecommendation, ObserverRegistrationConflictRequest>(
        Arg.Is<RecommendationDescription>(description => description.Value.Contains("Cratis:Chronicle:ApplicationVersion") && description.Value.Contains("Cratis__Chronicle__ApplicationVersion")),
        Arg.Is<ObserverRegistrationConflictRequest>(request => request.ObserverId == (ObserverId)_connectionDefinition.Identifier && request.IncomingEventSequenceId == _connectionDefinition.EventSequenceId &&
            request.OwnerEventSequenceId == _ownerKey.EventSequenceId && request.IncomingVersion == "1.0.0" && request.OwnerVersion == "2.0.0" && request.Outcome == "standby"));
}
