// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_there_is_no_existing_recommendation_of_same_type_and_request : given.all_dependencies
{
    RecommendationId _result;
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => _result = await _manager.Add<ITheRecommendation, TheRequest>("Some description", new TheRequest(42)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_return_a_new_id() => _result.ShouldNotEqual(RecommendationId.NotSet);
    [Fact] void should_initialize_the_recommendation_grain() => _theRecommendation.Received(1).Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<TheRequest>());
}
