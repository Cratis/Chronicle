// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_it_has_ignored_recommendations : given.an_observer
{
    readonly RecommendationId _replayId = RecommendationId.New();
    readonly RecommendationId _projectionId = RecommendationId.New();
    readonly RecommendationId _activeId = RecommendationId.New();
    readonly RecommendationId _otherObserverId = RecommendationId.New();
    readonly RecommendationId _otherSequenceId = RecommendationId.New();
    readonly RecommendationId _nullRequestId = RecommendationId.New();

    void Establish() => _recommendationStorage.GetAll().Returns(ImmutableList.Create(
        new RecommendationState
        {
            Id = _replayId,
            IsIgnored = true,
            Request = new ReplayCandidateRequest { ObserverId = _observerId, ObserverKey = _observerKey }
        },
        new RecommendationState
        {
            Id = _projectionId,
            IsIgnored = true,
            Request = new IgnoredProjectionRegistrationRequest { ProjectionId = _observerId.Value }
        },
        new RecommendationState
        {
            Id = _activeId,
            Request = new ReplayCandidateRequest { ObserverId = _observerId, ObserverKey = _observerKey }
        },
        new RecommendationState
        {
            Id = _otherObserverId,
            IsIgnored = true,
            Request = new ReplayCandidateRequest { ObserverId = "another-observer", ObserverKey = _observerKey with { ObserverId = "another-observer" } }
        },
        new RecommendationState
        {
            Id = _otherSequenceId,
            IsIgnored = true,
            Request = new ReplayCandidateRequest { ObserverId = _observerId, ObserverKey = _observerKey with { EventSequenceId = EventSequenceId.Outbox } }
        },
        new RecommendationState { Id = _nullRequestId, IsIgnored = true, Request = null! }));

    Task Because() => _observer.Remove();

    [Fact] void should_remove_the_ignored_replay_recommendation() => _recommendationStorage.Received(1).Remove(_replayId);
    [Fact] void should_remove_the_ignored_projection_registration() => _recommendationStorage.Received(1).Remove(_projectionId);
    [Fact] void should_keep_the_non_ignored_recommendation() => _recommendationStorage.DidNotReceive().Remove(_activeId);
    [Fact] void should_keep_the_other_observers_recommendation() => _recommendationStorage.DidNotReceive().Remove(_otherObserverId);
    [Fact] void should_keep_the_other_event_sequences_recommendation() => _recommendationStorage.DidNotReceive().Remove(_otherSequenceId);
    [Fact] void should_keep_the_unknown_request() => _recommendationStorage.DidNotReceive().Remove(_nullRequestId);
}
