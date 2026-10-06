// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_evaluating_definition_changes;

public class and_an_existing_tombstone_marker_changed : given.a_projection_replay_recommendation_evaluator
{
    EventType[] _result;
    readonly EventType _added = EventType2 with { Tombstone = true };

    void Because()
    {
        var from = new FromDefinition(new Dictionary<PropertyPath, string>(), "$eventSourceId", null);
        var previous = CreateDefinition(from: new Dictionary<EventType, FromDefinition> { [EventType1] = from });
        var current = CreateDefinition(from: new Dictionary<EventType, FromDefinition> { [EventType1 with { Tombstone = true }] = from, [_added] = from });
        _result = ProjectionReplayRecommendationEvaluator.GetAddedEventTypesIfOnlyEventTypesChanged(previous, current, ObjectComparer);
    }

    [Fact] void should_return_only_the_added_event_type() => _result.ShouldContainOnly(_added);
    [Fact] void should_preserve_the_added_types_marker() => _result.Single().Tombstone.ShouldBeTrue();
}
