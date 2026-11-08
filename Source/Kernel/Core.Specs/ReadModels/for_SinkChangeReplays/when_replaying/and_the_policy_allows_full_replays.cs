// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_the_policy_allows_full_replays : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    Task Because() => _replays.ReplayFor(_eventStore, _readModel);

    [Fact] void should_replay_the_projection_and_the_reducer_filling_it() => _observer.Received(2).Replay();
    [Fact] void should_ask_for_the_observer_of_the_projection() => _grainFactory.Received(1).GetGrain<Observation.IObserver>(Arg.Is<string>(_ => _.Contains("orders-projection")), Arg.Any<string>());
    [Fact] void should_not_replay_projections_of_other_read_models() => _grainFactory.DidNotReceive().GetGrain<Observation.IObserver>(Arg.Is<string>(_ => _.Contains("other-projection")), Arg.Any<string>());
    [Fact] void should_not_recommend_a_replay() => _recommendations.DidNotReceiveWithAnyArgs().Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
}
