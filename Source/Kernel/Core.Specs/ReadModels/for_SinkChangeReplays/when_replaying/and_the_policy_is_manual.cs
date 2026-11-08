// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_the_policy_is_manual : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    protected override DefinitionEvolutionPolicy Policy => DefinitionEvolutionPolicy.Manual;

    Task Because() => _replays.ReplayFor(_eventStore, _readModel);

    [Fact] void should_not_replay() => _observer.DidNotReceive().Replay();
    [Fact] void should_recommend_replaying_both() => _recommendations.ReceivedWithAnyArgs(2).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
}
