// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_replay_candidate_for_the_same_observer_has_other_reasons : given.all_dependencies
{
    readonly ObserverKey _observerKey = new("p", "event-store", "namespace", "log");

    void Establish()
    {
        var recommendation = Substitute.For<IReplayCandidateRecommendation>();
        _silo.AddProbe(_ => recommendation);
        _storedRecommendations.Add(new RecommendationState
        {
            Id = RecommendationId.New(),
            Type = (RecommendationType)typeof(IReplayCandidateRecommendation),
            Request = new ReplayCandidateRequest
            {
                ObserverId = _observerKey.ObserverId,
                ObserverKey = _observerKey,
                Reasons = [new ProjectionUpdateAvailableReplayCandidateReason()]
            },
            IsIgnored = true
        });
    }

    Task Because() => _manager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>("Some description", new()
    {
        ObserverId = _observerKey.ObserverId,
        ObserverKey = _observerKey,
        Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
    });

    [Fact] void should_keep_the_ignored_recommendation() => _recommendationStorage.DidNotReceive().Remove(Arg.Any<RecommendationId>());
}
