// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

public class a_manager_persisting_definition_changes : all_dependencies
{
    void Establish()
    {
        _silo.AddProbe<IReplayCandidateRecommendation>(identity =>
        {
            var id = (RecommendationId)Guid.Parse(identity.ToString().Split('+')[0]);
            var recommendation = Substitute.For<IReplayCandidateRecommendation>();
            recommendation.Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>()).Returns(call =>
            {
                _storedRecommendations.Add(new RecommendationState
                {
                    Id = id,
                    Type = (RecommendationType)typeof(IReplayCandidateRecommendation),
                    Request = call.Arg<ReplayCandidateRequest>()
                });
                return Task.CompletedTask;
            });
            return recommendation;
        });
        _recommendationStorage.Remove(Arg.Any<RecommendationId>()).Returns(call =>
        {
            _storedRecommendations.RemoveAll(row => row.Id == call.Arg<RecommendationId>());
            return Task.CompletedTask;
        });
    }

    protected Task<RecommendationId> Add(string changeId) => _manager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>("Definition changed", new()
    {
        ObserverId = "observer",
        ObserverKey = new("observer", "event-store", "namespace", "event-sequence"),
        ObserverType = ObserverType.Projection,
        Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()],
        ChangeId = changeId
    });
}
