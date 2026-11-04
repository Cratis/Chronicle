// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator.when_adding_event_types;

public class and_events_exist_before_the_next_position : given.an_additive_subscription
{
    void Establish() => Context = Context with { FirstEventSequenceNumberForAddedEventTypes = 9 };
    Task Because() => Evaluate();
    [Fact] async Task should_recommend_replay() => await Recommendations.Received(1).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
}
