// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_unignoring;

public class and_it_is_unknown : all_dependencies
{
    Exception _error;

    void Establish() => _recommendationStorage.Get(Arg.Any<RecommendationId>()).Returns((RecommendationState)null);

    async Task Because() => _error = await Catch.Exception(() => _manager.Unignore(RecommendationId.New()));

    [Fact] void should_fail_with_unknown_recommendation() => _error.ShouldBeOfExactType<UnknownRecommendation>();
    [Fact] void should_not_unignore_any_grain() => _theRecommendation.DidNotReceive().Unignore();
}
