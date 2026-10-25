// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_setting_the_definition;

public class and_the_reducer_cannot_be_replayed : given.a_reducer_grain_with_a_legacy_composite_table
{
    void Establish() => _observer.Replay().Returns(JobId.NotSet);

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_try_to_replay_the_reducer() => await _observer.Received(1).Replay();

    [Fact]
    async Task should_recommend_a_replay_with_the_key_format_reason() => await _recommendations.Received(1)
        .Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            Arg.Any<RecommendationDescription>(),
            Arg.Is<ReplayCandidateRequest>(_ => _.Reasons.Any(reason => reason is ReadModelKeyFormatChangedReplayCandidateReason)));
}
