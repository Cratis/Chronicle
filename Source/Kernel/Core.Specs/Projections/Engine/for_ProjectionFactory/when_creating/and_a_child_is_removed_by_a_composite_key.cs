// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_child_is_removed_by_a_composite_key : given.a_child_collection_projection
{
    const string CompositeKey = "$composite(tenantId=tenantId,itemId=itemId)";
    static readonly EventType _noteWritten = new("NoteWritten", EventTypeGeneration.First);
    static readonly EventType _noteRemoved = new("NoteRemoved", EventTypeGeneration.First);
    IProjection _projection;
    Changeset<AppendedEvent, ExpandoObject> _removing;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "notes":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"object","properties":{
                  "tenantId":{"type":"string"},"itemId":{"type":"string"}
                }},
                "text":{"type":"string"}
              }}}
            }}
            """);
        var notes = _definition.Children["notes"] with
        {
            From = new Dictionary<EventType, FromDefinition>(_definition.Children["notes"].From)
            {
                [_noteWritten] = new(
                    new Dictionary<PropertyPath, string> { ["text"] = "text" },
                    CompositeKey,
                    "$eventSourceId")
            },
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(_definition.Children["notes"].RemovedWith)
            {
                [_noteRemoved] = new(CompositeKey, "$eventSourceId")
            }
        };
        _definition = _definition with { Children = new Dictionary<PropertyPath, ChildrenDefinition>(_definition.Children) { ["notes"] = notes } };
    }

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var first = await Project(_noteWritten, "tenant", "item", new { id = "parent" }.AsExpandoObject());
        var second = await Project(_noteWritten, "other", "item", first.CurrentState);
        var third = await Project(_noteWritten, "tenant", "other", second.CurrentState);
        _removing = await Project(_noteRemoved, "tenant", "item", third.CurrentState);
    }

    async Task<Changeset<AppendedEvent, ExpandoObject>> Project(EventType eventType, string tenantId, string itemId, ExpandoObject state)
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("parent") with { EventType = eventType },
            new { tenantId, itemId, text = $"{tenantId}/{itemId}" }.AsExpandoObject());
        var keyResult = await _projection.GetKeyResolverFor(eventType)(_eventSequenceStorage, NullSink.Instance, incoming);
        var key = (keyResult as ResolvedKey)!.Key;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        var context = new ProjectionEventContext(key, incoming, changeset, _projection.GetOperationTypeFor(eventType), false);
        _projection.OnNext(context);
        var child = _projection.ChildProjections.Single();
        child.OnNext(context with { OperationType = child.GetOperationTypeFor(eventType) });
        return changeset;
    }

    [Fact] void should_record_one_child_removal() => _removing.Changes.Single().ShouldBeOfExactType<ChildRemoved>();
    [Fact] void should_record_the_matching_child() => ((IDictionary<string, object?>)_removing.Changes.OfType<ChildRemoved>().Single().Child)["text"].ShouldEqual("tenant/item");
    [Fact] void should_keep_the_children_with_different_key_parts() => ((IEnumerable<object>)((IDictionary<string, object?>)_removing.CurrentState)["notes"]!).Cast<IDictionary<string, object?>>().Select(_ => _["text"]).ShouldEqual(["other/item", "tenant/other"]);
}
