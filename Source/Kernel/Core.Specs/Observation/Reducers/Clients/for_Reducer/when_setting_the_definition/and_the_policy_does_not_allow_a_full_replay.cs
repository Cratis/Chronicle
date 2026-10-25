// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_setting_the_definition;

public class and_the_policy_does_not_allow_a_full_replay : given.a_reducer_grain_with_a_legacy_composite_table
{
    protected override DefinitionEvolutionPolicy Policy => DefinitionEvolutionPolicy.PartialOnly;

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_schedule_a_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] async Task should_not_replay_the_reducer() => await _observer.DidNotReceive().Replay();

    [Fact]
    async Task should_recommend_a_replay_with_the_key_format_reason() => await _recommendations.Received(1)
        .Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            Arg.Any<RecommendationDescription>(),
            Arg.Is<ReplayCandidateRequest>(_ => _.Reasons.Any(reason => reason is ReadModelKeyFormatChangedReplayCandidateReason)));
}
