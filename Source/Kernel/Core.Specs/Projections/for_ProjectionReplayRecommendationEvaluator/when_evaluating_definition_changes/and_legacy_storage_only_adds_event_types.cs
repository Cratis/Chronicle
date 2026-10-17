// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_evaluating_definition_changes;

public class and_legacy_storage_only_adds_event_types : given.a_projection_replay_recommendation_evaluator
{
    ProjectionDefinition _previous;
    ProjectionDefinition _current;
    EventType[] _result;
    DefinitionEvolutionPlan _plan;

    void Establish()
    {
        var every = new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false) { Key = "CompositeKey", AutoMap = AutoMap.Disabled };
        var child = new ChildrenDefinition(
            "id",
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            every,
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
        var oldEvery = every with { Key = Concepts.WellKnownExpressions.EventSourceId, AutoMap = AutoMap.Inherit };
        var oldChild = child with { All = oldEvery, FromEvery = oldEvery };
        _current = CreateDefinition(new Dictionary<EventType, FromDefinition>
        {
            [EventType1] = new(new Dictionary<PropertyPath, string> { ["name"] = "$event.name" }, "$eventSourceId", null),
            [EventType2] = new(new Dictionary<PropertyPath, string> { ["status"] = "$event.status" }, "$eventSourceId", null)
        }) with
        {
            Scope = ProjectionScope.Global,
            FromEvery = every,
            Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = child },
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = child },
            Version = "2.0.0"
        };
        _previous = _current with
        {
            HasLegacyStorageFields = true,
            Scope = ProjectionScope.Namespaced,
            FromEvery = oldEvery,
            Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = oldChild },
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = oldChild },
            From = _current.From.Where(pair => pair.Key != EventType2).ToDictionary(pair => pair.Key, pair => pair.Value),
            Version = "1.0.0"
        };
    }

    async Task Because()
    {
        _result = ProjectionReplayRecommendationEvaluator.GetAddedEventTypesIfOnlyEventTypesChanged(_previous, _current, ObjectComparer);
        var sequence = Substitute.For<IEventSequenceStorage>();
        sequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);
        _plan = _result.Length > 0
            ? await DefinitionEvolutionPlan.ForAddedEventTypes(sequence, _result, false)
            : DefinitionEvolutionPlan.FullReplay;
    }

    [Fact] void should_use_the_added_event_types_plan_instead_of_full_replay() => _result.ShouldContainOnly(EventType2);
    [Fact] void should_not_replay_without_historical_events_of_the_added_type() => _plan.Operation.ShouldEqual(DefinitionEvolutionOperation.NoAction);
    [Fact] void should_not_mutate_the_previous_definition() => _previous.HasLegacyStorageFields.ShouldBeTrue();
}
