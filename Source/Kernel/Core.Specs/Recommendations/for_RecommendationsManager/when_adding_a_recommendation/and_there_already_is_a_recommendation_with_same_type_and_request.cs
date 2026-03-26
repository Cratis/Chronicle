// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_there_already_is_a_recommendation_with_same_type_and_request : given.all_dependencies
{
    RecommendationId _existingId;
    RecommendationId _result;
    Exception _error;

    void Establish()
    {
        _existingId = RecommendationId.New();
        _storedRecommendations.Add(new RecommendationState
        {
            Id = _existingId,
            Type = (RecommendationType)typeof(ITheRecommendation),
            Request = new TheRequest(42)
        });
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _manager.Add<ITheRecommendation, TheRequest>("Some description", new TheRequest(42)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_return_the_existing_id() => _result.ShouldEqual(_existingId);
    [Fact] void should_not_initialize_a_new_recommendation_grain() => _theRecommendation.DidNotReceive().Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<TheRequest>());
}
