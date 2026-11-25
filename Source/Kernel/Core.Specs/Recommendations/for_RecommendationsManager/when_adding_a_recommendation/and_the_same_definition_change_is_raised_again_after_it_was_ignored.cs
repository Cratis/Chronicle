// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_the_same_definition_change_is_raised_again_after_it_was_ignored : given.a_definition_change
{
    void Establish() => Store(Request("a"));
    Task Because() => Add("a");

    [Fact] void should_return_the_ignored_recommendation() => _result.ShouldEqual(_ignoredId);
    [Fact] void should_not_initialize_a_recommendation() => _replay.DidNotReceive().Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
    [Fact] void should_not_remove_the_ignored_recommendation() => _recommendationStorage.DidNotReceive().Remove(_ignoredId);
}
