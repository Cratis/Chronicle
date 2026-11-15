// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_requesting : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    SinkChangeReplay _replay;

    async Task Because() => _replay = await _replays.Request(_eventStore, _readModel);

    [Fact] void should_record_a_replay_for_the_projection_and_the_reducer() => _recommendations.ReceivedWithAnyArgs(2).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
    [Fact] void should_not_record_replays_for_other_read_models() => _recommendations.DidNotReceive().Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(Arg.Any<Concepts.Recommendations.RecommendationDescription>(), Arg.Is<ReplayCandidateRequest>(_ => _.ObserverId == "other-projection"));
    [Fact] void should_not_replay_yet() => _observer.DidNotReceive().Replay();
    [Fact] void should_return_what_to_start() => _replay.Recommendations.Count.ShouldEqual(2);
}
