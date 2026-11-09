// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

/// <summary>
/// The recommendations manager returns the recommendation it already holds for an identical request, so requesting again after a
/// crash records nothing new.
/// </summary>
public class and_requesting_twice : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    SinkChangeReplay _first;
    SinkChangeReplay _second;

    async Task Because()
    {
        _first = await _replays.Request(_eventStore, _readModel);
        _second = await _replays.Request(_eventStore, _readModel);
    }

    [Fact] void should_refer_to_the_same_recommendations() => _second.Recommendations.Select(_ => _.Recommendation).ShouldEqual(_first.Recommendations.Select(_ => _.Recommendation));
}
