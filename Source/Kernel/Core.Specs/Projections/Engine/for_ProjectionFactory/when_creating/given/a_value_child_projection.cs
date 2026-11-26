// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_value_child_projection : a_child_collection_projection
{
    protected static readonly EventType _created = new("Created", EventTypeGeneration.First);
    protected static readonly EventType _valueAdded = new("ValueAdded", EventTypeGeneration.First);
    protected static readonly EventType _removed = new("Deleted", EventTypeGeneration.First);
    protected IProjection _projection;
    readonly Dictionary<string, ExpandoObject> _states = [];
    SetInitialState _setInitialState;

    protected virtual bool IncludeLegacyFrom => false;

    async Task Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "notes":{"type":"array","items":{"type":"string"}}
            }}
            """);
        var valueChild = Child() with
        {
            IdentifiedBy = PropertyPath.Root,
            FromEventProperty = new(_valueAdded, "value"),
            From = IncludeLegacyFrom
                ? new Dictionary<EventType, FromDefinition> { [_valueAdded] = new(new Dictionary<PropertyPath, string>(), "$eventSourceId", null) }
                : new Dictionary<EventType, FromDefinition>()
        };
        _definition = _definition with
        {
            From = new Dictionary<EventType, FromDefinition> { [_created] = new(new Dictionary<PropertyPath, string>(), "$eventSourceId", null) },
            Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["notes"] = valueChild },
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition> { [_removed] = new("$eventSourceId", null) }
        };
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var sink = Substitute.For<ISink>();
        sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(call =>
            Task.FromResult(_states.GetValueOrDefault(call.Arg<Concepts.Keys.Key>().Value.ToString()!)));
        _setInitialState = new(sink, NullLogger<SetInitialState>.Instance);
    }

    protected async Task<Changeset<AppendedEvent, ExpandoObject>> Project(EventType eventType, string value = "", string eventSourceId = "parent")
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(eventSourceId) with { EventType = eventType },
            new { value }.AsExpandoObject());
        var keyResult = await _projection.GetKeyResolverFor(eventType)(_eventSequenceStorage, NullSink.Instance, incoming);
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        var context = new ProjectionEventContext((keyResult as ResolvedKey)!.Key, incoming, changeset, _projection.GetOperationTypeFor(eventType), false);
        context = await _setInitialState.Perform(_projection, context);
        _projection.OnNext(context);
        foreach (var child in _projection.ChildProjections.Where(_ => _.EventTypes.Contains(eventType)))
        {
            child.OnNext(context with { OperationType = child.GetOperationTypeFor(eventType) });
        }
        if (changeset.Changes.OfType<Removed>().Any())
        {
            _states.Remove(eventSourceId);
        }
        else
        {
            _states[eventSourceId] = changeset.CurrentState;
        }
        return changeset;
    }

    protected static IEnumerable<object> Values(Changeset<AppendedEvent, ExpandoObject> changeset) =>
        (IEnumerable<object>)((IDictionary<string, object?>)changeset.CurrentState)["notes"]!;
}
