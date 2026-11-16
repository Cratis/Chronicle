// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_the_same_pending_definition_change_is_raised_again : given.a_manager_persisting_definition_changes
{
    RecommendationId _first;
    RecommendationId _second;

    async Task Because()
    {
        _first = await Add("a");
        _second = await Add("a");
    }

    [Fact] void should_reuse_the_pending_recommendation() => _second.ShouldEqual(_first);
    [Fact] void should_create_only_one_row() => _storedRecommendations.Count.ShouldEqual(1);
    [Fact] void should_keep_the_original_row() => _storedRecommendations.Single().Id.ShouldEqual(_first);
    [Fact] void should_not_ignore_the_recommendation() => _storedRecommendations.Single().IsIgnored.ShouldBeFalse();
    [Fact] void should_not_remove_any_rows() => _recommendationStorage.DidNotReceive().Remove(Arg.Any<RecommendationId>());
}
