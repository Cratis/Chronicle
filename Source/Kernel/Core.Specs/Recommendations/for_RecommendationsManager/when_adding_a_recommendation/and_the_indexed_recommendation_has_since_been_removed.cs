// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_the_indexed_recommendation_has_since_been_removed : given.a_definition_change
{
    RecommendationId _first;

    async Task Because()
    {
        await Add("a");
        _first = _result;
        Store(Request("a"));
        _storedRecommendations[0].Id = _first;
        await Add("a");
        _storedRecommendations.Clear();
        await Add("a");
    }

    [Fact] void should_create_a_new_recommendation() => _result.ShouldNotEqual(_first);
    [Fact] void should_initialize_again() => _replay.Received(2).Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
    [Fact] void should_verify_the_indexed_id() => _recommendationStorage.Received().Get(_first);
    [Fact] void should_not_reload_the_index() => _recommendationStorage.Received(1).GetAll();
}
