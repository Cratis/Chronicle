// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_the_stored_type_is_marked_as_a_tombstone : given.a_projection_with_a_history
{
    IEnumerable<ReadModelSnapshot> _result;

    void Establish()
    {
        var eventType = new EventType("my-event", EventTypeGeneration.First);
        var schema = new JsonSchema();
        schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
            }
        };
        var eventStore = _storage.GetEventStore("test-store");
        eventStore.EventTypes.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([
            new EventTypeSchema(eventType with { Tombstone = true }, EventTypeOwner.Client, EventTypeSource.Code, schema)
        ]);

        dynamic encrypted = new ExpandoObject();
        encrypted.name = "encrypted-name";
        var appendedEvent = new Concepts.Events.AppendedEvent(
            Concepts.Events.EventContext.EmptyWithEventSourceId("my-instance") with
            {
                EventType = eventType,
                Subject = new Subject("my-instance"),
                SequenceNumber = EventSequenceNumber.First
            },
            encrypted);
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns([appendedEvent]);
        eventStore.GetNamespace("test-namespace").GetEventSequence("event-log")
            .GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);

        var metadataManager = Substitute.For<IJsonSchemaMetadataManager>();
        metadataManager.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), schema, "my-instance", Arg.Any<JsonObject>())
            .Returns(_ => Task.FromResult(new JsonObject { ["name"] = "decrypted-name" }));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), schema)
            .Returns(_ => new JsonObject { ["name"] = "encrypted-name" });
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), schema)
            .Returns(call =>
            {
                dynamic content = new ExpandoObject();
                content.name = call.ArgAt<JsonObject>(0)["name"]!.GetValue<string>();
                return (ExpandoObject)content;
            });
        _eventCompliance = new EventCompliance(metadataManager, _expandoObjectConverter);
        _projection.ProcessForSingleReadModel(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<Concepts.Events.AppendedEvent>>(2).Single().Content));
    }

    async Task Because() => _result = await AllSnapshots(nameof(ReadModelSnapshotGrouping.Event));

    [Fact] void should_release_the_event_content_in_the_snapshot() => JsonNode.Parse(_result.Single().Events.Single().Content)!["name"]!.GetValue<string>().ShouldEqual("decrypted-name");
}
