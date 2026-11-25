// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_a_pending_definition_change_is_followed_by_another_definition_change : given.a_manager_persisting_definition_changes
{
    RecommendationId _first;
    RecommendationId _second;

    async Task Because()
    {
        _first = await Add("a");
        _second = await Add("b");
    }

    [Fact] void should_remove_the_previous_pending_recommendation() => _recommendationStorage.Received(1).Remove(_first);
    [Fact] void should_leave_only_one_pending_recommendation() => _storedRecommendations.Count.ShouldEqual(1);
    [Fact] void should_keep_the_new_recommendation() => _storedRecommendations.Single().Id.ShouldEqual(_second);
    [Fact] void should_keep_only_the_new_definition_change() => ((ReplayCandidateRequest)_storedRecommendations.Single().Request).ChangeId.ShouldEqual("b");
    [Fact] void should_not_ignore_the_new_recommendation() => _storedRecommendations.Single().IsIgnored.ShouldBeFalse();
}
