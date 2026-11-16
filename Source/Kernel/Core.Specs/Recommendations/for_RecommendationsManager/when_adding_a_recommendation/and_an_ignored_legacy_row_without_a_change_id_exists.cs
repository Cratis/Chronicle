// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_legacy_row_without_a_change_id_exists : given.an_ignored_legacy_definition_change
{
    Task Because() => Add(string.Empty);

    [Fact] void should_deserialize_with_an_empty_change_id() => _legacy.ChangeId.ShouldEqual(string.Empty);

    [Fact]
    async Task should_still_suppress_an_identical_legacy_request()
    {
        _result.ShouldEqual(_ignoredId);
        await _replay.DidNotReceive().Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
        await _recommendationStorage.DidNotReceive().Remove(_ignoredId);
    }
}
