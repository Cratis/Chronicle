// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ParticipatingEventTypes;

public class when_selecting_schemas : Specification
{
    static readonly EventType _from = new("FromEvent", EventTypeGeneration.First);
    static readonly EventType _join = new("JoinEvent", EventTypeGeneration.First);
    static readonly EventType _childFrom = new("ChildEvent", EventTypeGeneration.First);
    static readonly EventType _removed = new("RemovedEvent", EventTypeGeneration.First);
    static readonly EventType _unrelated = new("UnrelatedEvent", EventTypeGeneration.First);

    IReadOnlyList<EventTypeSchema> _all;
    IReadOnlyList<EventTypeSchema> _selected;
    IReadOnlyList<EventTypeSchema> _selectedForAllEvents;

    void Establish() => _all = new[] { _from, _join, _childFrom, _removed, _unrelated }
        .Select(type => new EventTypeSchema(type, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()))
        .ToArray();

    void Because()
    {
        var child = new ChildrenDefinition(
            "id",
            new Dictionary<EventType, FromDefinition> { [_childFrom] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            Concepts.EventSequences.EventSequenceId.Log,
            "projection",
            "model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition> { [_from] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) },
            new Dictionary<EventType, JoinDefinition> { [_join] = new("id", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet) },
            new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = child },
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition> { [_removed] = new(PropertyExpression.NotSet, null) },
            new Dictionary<EventType, RemovedWithJoinDefinition>());

        _selected = ParticipatingEventTypes.SchemasFor(definition, _all);
        _selectedForAllEvents = ParticipatingEventTypes.SchemasFor(definition with { SubscribesToAllEvents = true }, _all);
    }

    [Fact] void should_keep_the_schemas_of_from_join_child_and_removal_event_types() => _selected.Select(_ => _.Type.Id).ShouldContainOnly(_from.Id, _join.Id, _childFrom.Id, _removed.Id);
    [Fact] void should_drop_the_schema_of_an_unrelated_event_type() => _selected.Any(_ => _.Type.Id == _unrelated.Id).ShouldBeFalse();
    [Fact] void should_keep_every_schema_when_subscribing_to_all_events() => _selectedForAllEvents.Count.ShouldEqual(_all.Count);
}
