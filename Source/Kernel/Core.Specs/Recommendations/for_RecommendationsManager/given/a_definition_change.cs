// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

public class a_definition_change : all_dependencies
{
    protected RecommendationId _ignoredId = RecommendationId.New();
    protected IReplayCandidateRecommendation _replay;
    protected RecommendationId _result;

    void Establish()
    {
        _replay = Substitute.For<IReplayCandidateRecommendation>();
        _silo.AddProbe(_ => _replay);
    }

    protected ReplayCandidateRequest Request(string changeId) => new()
    {
        ObserverId = "observer",
        ObserverKey = new("observer", "event-store", "namespace", "event-sequence"),
        ObserverType = ObserverType.Projection,
        Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()],
        ChangeId = changeId
    };

    protected void Store(ReplayCandidateRequest request) => _storedRecommendations.Add(new RecommendationState
    {
        Id = _ignoredId,
        IsIgnored = true,
        Type = (RecommendationType)typeof(IReplayCandidateRecommendation),
        Request = request
    });

    protected async Task Add(string changeId) => _result = await _manager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>("Definition changed", Request(changeId));
}
