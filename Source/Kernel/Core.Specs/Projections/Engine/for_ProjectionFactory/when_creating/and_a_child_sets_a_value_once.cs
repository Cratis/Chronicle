// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_child_sets_a_value_once : given.a_child_collection_projection
{
    static readonly EventType _noteWritten = new("NoteWritten", EventTypeGeneration.First);
    IProjection _projection;
    object? _firstAuthorOnCreate;
    object? _authorAfterSecondEvent;
    object? _textAfterSecondEvent;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "notes":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"integer"},"firstAuthor":{"type":"string"},"text":{"type":"string"}
              }}}
            }}
            """);
        _definition.Children["notes"].From[_noteWritten] = new(
            new Dictionary<PropertyPath, string>
            {
                ["firstAuthor"] = $"{WellKnownExpressions.SetOnce}(author)",
                ["text"] = "text"
            },
            "noteId",
            "$eventSourceId");
    }

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var creating = await Project("alice", "first", new { id = "parent" }.AsExpandoObject());
        _firstAuthorOnCreate = ((IDictionary<string, object?>)creating.Changes.OfType<ChildAdded>().Single().Child)["firstAuthor"];
        var updating = await Project("bob", "second", creating.CurrentState);
        var note = ((IEnumerable<object>)((IDictionary<string, object?>)updating.CurrentState)["notes"]!).Cast<IDictionary<string, object?>>().Single();
        _authorAfterSecondEvent = note["firstAuthor"];
        _textAfterSecondEvent = note["text"];
    }

    async Task<Changeset<AppendedEvent, ExpandoObject>> Project(string author, string text, ExpandoObject state)
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("parent") with { EventType = _noteWritten },
            new { noteId = 42, author, text }.AsExpandoObject());
        var keyResult = await _projection.GetKeyResolverFor(_noteWritten)(_eventSequenceStorage, NullSink.Instance, incoming);
        var key = (keyResult as ResolvedKey)!.Key;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        var context = new ProjectionEventContext(key, incoming, changeset, _projection.GetOperationTypeFor(_noteWritten), false);
        _projection.OnNext(context);
        var child = _projection.ChildProjections.Single();
        child.OnNext(context with { OperationType = child.GetOperationTypeFor(_noteWritten) });
        return changeset;
    }

    [Fact] void should_set_the_value_when_the_child_is_added() => _firstAuthorOnCreate.ShouldEqual("alice");
    [Fact] void should_keep_the_first_value_on_a_later_event() => _authorAfterSecondEvent.ShouldEqual("alice");
    [Fact] void should_still_set_other_values_on_a_later_event() => _textAfterSecondEvent.ShouldEqual("second");
}
