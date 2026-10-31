// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_unignoring;

public class and_it_exists : all_dependencies
{
    RecommendationId _id;
    Exception _error;

    void Establish()
    {
        _id = RecommendationId.New();
        _recommendationStorage.Get(_id).Returns(new RecommendationState
        {
            Id = _id,
            Type = (RecommendationType)typeof(ITheRecommendation),
            Request = new TheRequest(42),
            IsIgnored = true
        });
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.Unignore(_id));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_unignore_the_recommendation_grain() => _theRecommendation.Received(1).Unignore();
}
