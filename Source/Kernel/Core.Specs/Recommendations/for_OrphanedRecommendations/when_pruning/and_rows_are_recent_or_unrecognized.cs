// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_rows_are_recent_or_unrecognized : given.a_pruner
{
    void Establish()
    {
        var boundary = Replay("boundary");
        boundary.Occurred = _now.AddHours(-1);
        var recent = Registration("recent");
        recent.Occurred = _now.AddMinutes(-10);
        var unknown = Replay("unknown");
        unknown.Request = Substitute.For<IRecommendationRequest>();
        var missingRequest = new RecommendationState { Occurred = _now.AddDays(-1) };
        _rows.AddRange([boundary, recent, unknown, missingRequest]);
    }

    Task Because() => Prune();

    [Fact] void should_keep_all_rows() => _removed.ShouldBeEmpty();
    [Fact] async Task should_not_load_observers() => await _observers.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverId>());
}
