// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_evaluating_definition_changes;

public class and_only_new_event_types_are_added : given.a_projection_replay_recommendation_evaluator
{
    EventType[] _result = [];

    void Establish()
    {
        var previous = CreateDefinition(
            from: new Dictionary<EventType, FromDefinition>
            {
                [EventType1] = new(new Dictionary<PropertyPath, string> { ["name"] = "$event.name" }, "$eventSourceId", null)
            });

        var current = CreateDefinition(
            from: new Dictionary<EventType, FromDefinition>
            {
                [EventType1] = new(new Dictionary<PropertyPath, string> { ["name"] = "$event.name" }, "$eventSourceId", null),
                [EventType2] = new(new Dictionary<PropertyPath, string> { ["name"] = "$event.name" }, "$eventSourceId", null)
            });

        _result = ProjectionReplayRecommendationEvaluator.GetAddedEventTypesIfOnlyEventTypesChanged(previous, current, ObjectComparer);
    }

    [Fact] void should_return_the_added_event_type() => _result.ShouldContain(EventType2);
}
