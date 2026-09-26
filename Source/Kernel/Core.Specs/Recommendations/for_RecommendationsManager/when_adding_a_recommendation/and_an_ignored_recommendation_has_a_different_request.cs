// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_recommendation_has_a_different_request : all_dependencies
{
    Exception _error;

    void Establish() => _storedRecommendations.Add(new RecommendationState
    {
        Id = RecommendationId.New(),
        Type = (RecommendationType)typeof(ITheRecommendation),
        Request = new TheRequest(42),
        IsIgnored = true
    });

    async Task Because() => _error = await Catch.Exception(async () => await _manager.Add<ITheRecommendation, TheRequest>("Some description", new TheRequest(43)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_raise_it_as_the_new_recommendation_it_is() => _theRecommendation.Received(1).Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<TheRequest>());
}
