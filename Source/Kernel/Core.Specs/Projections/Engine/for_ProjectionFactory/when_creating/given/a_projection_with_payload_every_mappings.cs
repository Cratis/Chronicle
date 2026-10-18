// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_projection_with_payload_every_mappings : a_child_collection_projection
{
    protected static readonly EventType _valueChanged = new("ValueChanged", EventTypeGeneration.First);
    protected static readonly EventType _unrelated = new("Unrelated", EventTypeGeneration.First);
    protected static readonly EventType _childUpdated = new("ChildUpdated", EventTypeGeneration.First);
    protected IProjection _projection;
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;
    protected AppendedEvent _incoming;
    protected IDictionary<string, object?> _result;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},"lastValue":{"type":"string"},"count":{"type":"integer","format":"int64"},
              "notes":{"type":"array","items":{"type":"object","properties":{"id":{"type":"integer"}}}}
            }}
            """);
        _definition = _definition with
        {
            FromEvery = new(new Dictionary<PropertyPath, string> { ["lastValue"] = "value", ["count"] = "$count" }, true)
        };
        _definition.From[_valueChanged] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null);
        _definition.From[_unrelated] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null);
        _definition.Children["notes"].From[_childUpdated] = new(new Dictionary<PropertyPath, string>(), "noteId", "parentId");
        var parent = new AppendedEvent(EventContext.EmptyWithEventSourceId("parent") with { EventType = _valueChanged }, new ExpandoObject());
        _eventSequenceStorage.TryGetLastInstanceOfAny("parent", Arg.Any<IEnumerable<EventTypeId>>()).Returns(new Option<AppendedEvent>(parent));
    }

    protected async Task Project(EventType eventType, ExpandoObject content)
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        _incoming = new(EventContext.EmptyWithEventSourceId("parent") with { EventType = eventType }, content);
        var keyResult = await _projection.GetKeyResolverFor(eventType)(_eventSequenceStorage, NullSink.Instance, _incoming);
        var key = (keyResult as ResolvedKey)!.Key;
        _changeset = new(new ObjectComparer(), _incoming, new { id = "parent", lastValue = "kept", count = 0L, notes = new[] { new { id = 42 } } }.AsExpandoObject());
        _projection.OnNext(new ProjectionEventContext(key, _incoming, _changeset, _projection.GetOperationTypeFor(eventType), false));
        _result = _changeset.CurrentState;
    }
}
