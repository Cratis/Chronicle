// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator.when_adding_event_types;

public class and_other_event_types_are_removed : given.an_additive_subscription
{
    void Establish() => Context = Context with { Definition = Context.Definition with { EventTypes = [new("original", 1), new("removed", 1)] } };
    Task Because() => Evaluate();
    [Fact] async Task should_keep_the_replay_recommendation() => await Recommendations.Received(1).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());
}
