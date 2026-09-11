// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_determining_partial_replay_safety;

public class and_added_properties_are_independent : given.a_projection_replay_recommendation_evaluator
{
    bool _result;

    void Because()
    {
        var previous = CreateDefinition(from: new Dictionary<EventType, FromDefinition>
        {
            [EventType1] = new(new Dictionary<PropertyPath, string> { ["name"] = "name" }, PropertyExpression.NotSet, null)
        }) with { AutoMap = AutoMap.Disabled };
        var current = CreateDefinition(from: new Dictionary<EventType, FromDefinition>
        {
            [EventType1] = previous.From[EventType1],
            [EventType2] = new(new Dictionary<PropertyPath, string> { ["description"] = "description" }, PropertyExpression.NotSet, null)
        }) with { AutoMap = AutoMap.Disabled };

        _result = ProjectionReplayRecommendationEvaluator.CanPartiallyReplay(previous, current, [EventType2]);
    }

    [Fact] void should_allow_partial_replay() => _result.ShouldBeTrue();
}
