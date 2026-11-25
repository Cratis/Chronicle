// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_a_new_change_supersedes_an_ignored_legacy_row : given.an_ignored_legacy_definition_change
{
    Task Because() => Add("x");

    [Fact] void should_recommend_a_new_change() => _result.ShouldNotEqual(_ignoredId);
    [Fact] Task should_initialize_the_new_change() => _replay.Received(1).Initialize(Arg.Any<RecommendationDescription>(), Arg.Is<ReplayCandidateRequest>(request => request.ChangeId == "x"));
    [Fact] Task should_prune_the_legacy_row() => _recommendationStorage.Received(1).Remove(_ignoredId);
}
