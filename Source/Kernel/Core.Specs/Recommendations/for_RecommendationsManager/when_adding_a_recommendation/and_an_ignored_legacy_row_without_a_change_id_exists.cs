// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_legacy_row_without_a_change_id_exists : given.a_definition_change
{
    ReplayCandidateRequest _legacy;

    void Establish()
    {
        var json = JsonSerializer.SerializeToNode(Request(string.Empty))!.AsObject();
        json.Remove(nameof(ReplayCandidateRequest.ChangeId));
        _legacy = json.Deserialize<ReplayCandidateRequest>()!;
        Store(_legacy);
    }

    [Fact] void should_deserialize_with_an_empty_change_id() => _legacy.ChangeId.ShouldEqual(string.Empty);

    [Fact]
    async Task should_still_suppress_an_identical_legacy_request()
    {
        await Add(string.Empty);
        _result.ShouldEqual(_ignoredId);
        await _replay.DidNotReceive().Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
        await _recommendationStorage.DidNotReceive().Remove(_ignoredId);
    }

    [Fact]
    async Task should_recommend_a_new_change_and_prune_the_legacy_row()
    {
        await Add("x");
        _result.ShouldNotEqual(_ignoredId);
        await _replay.Received(1).Initialize(Arg.Any<RecommendationDescription>(), Arg.Is<ReplayCandidateRequest>(request => request.ChangeId == "x"));
        await _recommendationStorage.Received(1).Remove(_ignoredId);
    }
}
