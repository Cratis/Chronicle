// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_the_observer_refuses : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    void Establish() => _observer.Replay().Returns<Task<Cratis.Choreography.Jobs.JobId>>(_ => throw new InvalidOperationException("refused"));

    Task Because() => _replays.ReplayFor(_eventStore, _readModel);

    [Fact] void should_recommend_replaying_instead() => _recommendations.ReceivedWithAnyArgs(2).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
}
