// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_definition_change_is_followed_by_another_definition_change : given.a_definition_change
{
    void Establish() => Store(Request("a"));
    Task Because() => Add("b");

    [Fact] void should_create_a_new_recommendation() => _result.ShouldNotEqual(_ignoredId);
    [Fact] void should_initialize_the_new_recommendation() => _replay.Received(1).Initialize(Arg.Any<RecommendationDescription>(), Arg.Is<ReplayCandidateRequest>(request => request.ChangeId == "b"));
    [Fact] void should_remove_the_superseded_ignored_recommendation() => _recommendationStorage.Received(1).Remove(_ignoredId);
}
