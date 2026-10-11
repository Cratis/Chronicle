// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_projection_with_every_event_mappings_and_children : a_child_collection_projection
{
    protected static readonly EventType _noteUpdated = new("NoteUpdated", EventTypeGeneration.First);
    protected static readonly EventType _replyUpdated = new("ReplyUpdated", EventTypeGeneration.First);
    protected static readonly DateTimeOffset _occurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected IProjection _projection;
    protected Key _key;
    protected IDictionary<string, object?> _result;
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;
    protected bool _noteExists = true;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},"count":{"type":"integer","format":"int64"},"lastUpdated":{"type":"string","format":"date-time"},
              "notes":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"integer"},"count":{"type":"integer","format":"int64"},
                "replies":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"}}}}
              }}}
            }}
            """);
        _definition = _definition with
        {
            FromEvery = new(
                new Dictionary<PropertyPath, string>
                {
                    ["count"] = "$count",
                    ["lastUpdated"] = "$eventContext(occurred)"
                },
                true)
        };
        _definition.Children["notes"].From[_noteUpdated] = new(new Dictionary<PropertyPath, string>(), "noteId", "parentId");
    }

    protected async Task ProjectChildEvent(bool nested = false)
    {
        var context = await CreateChildEventContext(nested);
        _projection.OnNext(context);
        var child = _projection.ChildProjections.Single();
        child.OnNext(context with { OperationType = child.GetOperationTypeFor(context.Event.Context.EventType) });
        if (nested)
        {
            child.ChildProjections.Single().OnNext(context with { OperationType = ProjectionOperationType.From });
        }
        _result = _changeset.CurrentState;
    }

    protected async Task<ProjectionEventContext> CreateChildEventContext(bool nested = false)
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("child-stream") with { EventType = nested ? _replyUpdated : _noteUpdated, Occurred = _occurred, SequenceNumber = 2 },
            new { parentId = "parent", noteId = 42, replyId = "reply" }.AsExpandoObject());
        var parentEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("parent") with { EventType = _noteUpdated, SequenceNumber = 1 },
            new { parentId = "parent", noteId = 42 }.AsExpandoObject());
        _eventSequenceStorage.TryGetLastInstanceOfAny("42", Arg.Is<IEnumerable<EventTypeId>>(_ => _.Contains(_noteUpdated.Id)))
            .Returns(new Option<AppendedEvent>(parentEvent));
        var keyResult = await _projection.GetKeyResolverFor(incoming.Context.EventType)(_eventSequenceStorage, NullSink.Instance, incoming);
        _key = (keyResult as ResolvedKey)!.Key;
        var state = new
        {
            id = "parent",
            count = 0L,
            lastUpdated = DateTimeOffset.MinValue,
            notes = _noteExists ? new[] { new { id = 42, count = 0L, replies = new[] { new { id = "reply" } } } } : []
        }.AsExpandoObject();
        _changeset = new(new ObjectComparer(), incoming, state);
        return new ProjectionEventContext(_key, incoming, _changeset, _projection.GetOperationTypeFor(incoming.Context.EventType), false);
    }
}
