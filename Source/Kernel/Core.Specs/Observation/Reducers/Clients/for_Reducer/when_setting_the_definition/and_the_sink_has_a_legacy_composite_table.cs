// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_setting_the_definition;

public class and_the_sink_has_a_legacy_composite_table : given.a_reducer_grain_with_a_legacy_composite_table
{
    void Establish() => _observer.Replay().Returns(JobId.New());

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_replay_the_reducer() => await _observer.Received(1).Replay();
    [Fact] void should_dispose_the_replay_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);

    [Fact]
    async Task should_not_recommend_a_replay() => await _recommendations.DidNotReceive()
        .Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
}
