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

public class and_child_every_mappings_apply_on_the_creating_event : given.a_child_collection_projection
{
    static readonly EventType _childAdded = new("ChildAdded", EventTypeGeneration.First);
    static readonly EventType _childUpdated = new("ChildUpdated", EventTypeGeneration.First);
    IProjection _projection;
    IDictionary<string, object?> _result;
    object? _createdValue;
    bool _createdChildHasSource;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},"source":{"type":"string"},"lastValue":{"type":"string"},
              "notes":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"integer"},"lastValue":{"type":"string"}
              }}}
            }}
            """);
        var all = new FromEveryDefinition(new Dictionary<PropertyPath, string> { ["lastValue"] = "value" }, true);
        _definition.Children["notes"] = Child() with { All = all, FromEvery = all };
        _definition.Children["notes"].From[_childAdded] = new(new Dictionary<PropertyPath, string>(), "noteId", "$eventSourceId");
        _definition.Children["notes"].From[_childUpdated] = new(new Dictionary<PropertyPath, string>(), "noteId", "$eventSourceId");
        _definition = _definition with
        {
            FromEvery = new(new Dictionary<PropertyPath, string> { ["source"] = "$eventContext(eventSourceId)" }, true)
        };
    }

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var state = new { id = "parent", notes = new[] { new { id = 7, lastValue = "existing" } } }.AsExpandoObject();
        var creating = await Project(_childAdded, "child-value", state);
        var child = (IDictionary<string, object?>)creating.Changes.OfType<ChildAdded>().Single().Child;
        _createdValue = child["lastValue"];
        _createdChildHasSource = child.ContainsKey("source");
        _result = (await Project(_childUpdated, "updated-value", creating.CurrentState)).CurrentState;
    }

    async Task<Changeset<AppendedEvent, ExpandoObject>> Project(EventType eventType, string value, ExpandoObject state)
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("parent") with { EventType = eventType },
            new { noteId = 42, value }.AsExpandoObject());
        var keyResult = await _projection.GetKeyResolverFor(eventType)(_eventSequenceStorage, NullSink.Instance, incoming);
        var key = (keyResult as ResolvedKey)!.Key;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        var context = new ProjectionEventContext(key, incoming, changeset, _projection.GetOperationTypeFor(eventType), false);
        _projection.OnNext(context);
        var child = _projection.ChildProjections.Single();
        child.OnNext(context with { OperationType = child.GetOperationTypeFor(eventType) });
        return changeset;
    }

    [Fact] void should_map_the_creating_event_into_the_new_child() => _createdValue.ShouldEqual("child-value");
    [Fact] void should_map_later_events_into_the_same_child() => ((IEnumerable<object>)_result["notes"]!).Cast<IDictionary<string, object?>>().Single(child => (int)child["id"]! == 42)["lastValue"].ShouldEqual("updated-value");
    [Fact] void should_not_change_a_different_existing_child() => ((IEnumerable<object>)_result["notes"]!).Cast<IDictionary<string, object?>>().Single(child => (int)child["id"]! == 7)["lastValue"].ShouldEqual("existing");
    [Fact] void should_keep_the_root_context_mapping() => _result["source"].ShouldEqual("parent");
    [Fact] void should_not_set_the_root_from_the_child_mapping() => _result.ContainsKey("lastValue").ShouldBeFalse();
    [Fact] void should_not_leak_the_root_context_mapping_into_the_added_child() => _createdChildHasSource.ShouldBeFalse();
}
