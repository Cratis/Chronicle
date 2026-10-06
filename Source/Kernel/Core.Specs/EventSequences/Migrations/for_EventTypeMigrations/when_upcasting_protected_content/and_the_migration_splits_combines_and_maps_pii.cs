// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_upcasting_protected_content;

public class and_the_migration_splits_combines_and_maps_pii : Cratis.Chronicle.EventSequences.for_EventSequence.given.an_event_sequence
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _targetSchema;
    protected JsonObject _released;
    protected JsonObject _protectedTarget;
    protected JsonObject _erased;
    JsonObject _content;
    ExpandoObjectConverter _converter;
    InMemoryEncryptionKeyStorage _keys;

    protected virtual bool ThroughBatch => false;
    protected virtual bool FromStoredContent => false;

    async Task Establish()
    {
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        var migrations = new EventTypeMigrations(_storage, _converter);
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<Concepts.EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>()).Returns(call => migrations.MigrateToAllGenerations(call.ArgAt<Concepts.EventStoreName>(0), call.ArgAt<EventType>(1), call.ArgAt<JsonObject>(2), call.ArgAt<ExpandoObject>(3)));
        var sourceSchema = await JsonSchema.FromJsonAsync(
            """
            {
              "type":"object",
              "properties": {
                "name": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "firstName": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "lastName": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "status": {"type":"string","compliance":[{"metadataType":"PII","details":""}]}
              }
            }
            """);
        _targetSchema = await JsonSchema.FromJsonAsync(
            """
            {
              "type":"object",
              "properties": {
                "splitFirst": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "splitLast": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "combined": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "mapped": {"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "defaulted": {"type":"string","compliance":[{"metadataType":"PII","details":""}]}
              }
            }
            """);
        var upcast = JsonNode.Parse(
            """
            {
              "splitFirst":{"$split":{"source":"name","separator":" ","part":0}},
              "splitLast":{"$split":{"source":"name","separator":" ","part":1}},
              "combined":{"$combine":{"sources":["firstName","lastName"],"separator":" "}},
              "mapped":{"$mapValues":{"source":"status","mappings":[{"from":"old","to":"new"}]}},
              "defaulted":{"$defaultValue":"personal default"}
            }
            """)!.AsObject();
        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.None,
            false,
            [new EventTypeGenerationDefinition(1, sourceSchema), new EventTypeGenerationDefinition(2, _targetSchema)],
            [new EventTypeMigrationDefinition(1, 2, [], upcast, new JsonObject())]));
        _eventTypesStorage.GetFor(_eventType.Id, 1).Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, sourceSchema));
        _eventTypesStorage.GetFor(_eventType.Id, 2).Returns(new EventTypeSchema(new EventType(_eventType.Id, 2), EventTypeOwner.Server, EventTypeSource.Code, _targetSchema));
        _complianceManager.Apply(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => _manager.Apply(call.ArgAt<Concepts.EventStoreName>(0), call.ArgAt<Concepts.EventStoreNamespaceName>(1), call.ArgAt<JsonSchema>(2), call.ArgAt<string>(3), call.ArgAt<JsonObject>(4)));
        _complianceManager.Release(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => _manager.Release(call.ArgAt<Concepts.EventStoreName>(0), call.ArgAt<Concepts.EventStoreNamespaceName>(1), call.ArgAt<JsonSchema>(2), call.ArgAt<string>(3), call.ArgAt<JsonObject>(4)));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(call => _converter.ToExpandoObject(call.ArgAt<JsonObject>(0), call.ArgAt<JsonSchema>(1)));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(call => _converter.ToJsonObject(call.ArgAt<ExpandoObject>(0), call.ArgAt<JsonSchema>(1)));
        _content = JsonNode.Parse("""{"name":"Jane Austen","firstName":"Jane","lastName":"Austen","status":"old"}""")!.AsObject();
    }

    async Task Because()
    {
        IDictionary<EventTypeGeneration, ExpandoObject> migrated;
        if (FromStoredContent)
        {
            var sourceSchema = await _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation);
            var stored = await _manager.Apply(EventStore, EventStoreNamespace, sourceSchema.Schema, "author-subject", _content);
            migrated = await new ProtectedEventTypeMigrations(_eventTypesStorage, _eventTypeMigrations, _manager, _converter)
                .Migrate(EventStore, EventStoreNamespace, _eventType, stored, _converter.ToExpandoObject(stored, sourceSchema.Schema), "author-subject");
        }
        else if (ThroughBatch)
        {
            await _eventSequence.AppendMany([new EventToAppend(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, [], _content, Subject: "author-subject")], CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
            migrated = ((IEnumerable<Storage.EventSequences.EventToAppendToStorage>)_eventSequenceStorage.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(Storage.EventSequences.IEventSequenceStorage.AppendMany)).GetArguments()[0]!).Single().GenerationalContent!;
        }
        else
        {
            await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, _content, CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None, subject: "author-subject");
            migrated = (IDictionary<EventTypeGeneration, ExpandoObject>)_eventSequenceStorage.ReceivedCalls()
                .Single(call => call.GetMethodInfo().Name == nameof(Storage.EventSequences.IEventSequenceStorage.Append)).GetArguments()[11]!;
        }

        _protectedTarget = _converter.ToJsonObject(migrated[(EventTypeGeneration)2], _targetSchema);
        _released = await _manager.Release(
            EventStore,
            EventStoreNamespace,
            _targetSchema,
            "author-subject",
            _protectedTarget);
        await _keys.RecordErasureFor(EventStore, EventStoreNamespace, "author-subject");
        await _keys.DeleteFor(EventStore, EventStoreNamespace, "author-subject");
        _erased = await _manager.Release(EventStore, EventStoreNamespace, _targetSchema, "author-subject", _protectedTarget);
    }

    [Fact] void should_split_the_first_name_from_plaintext() => _released["splitFirst"]!.GetValue<string>().ShouldEqual("Jane");
    [Fact] void should_split_the_last_name_from_plaintext() => _released["splitLast"]!.GetValue<string>().ShouldEqual("Austen");
    [Fact] void should_combine_the_plaintext_names() => _released["combined"]!.GetValue<string>().ShouldEqual("Jane Austen");
    [Fact] void should_map_the_plaintext_value() => _released["mapped"]!.GetValue<string>().ShouldEqual("new");
    [Fact] void should_protect_default_values_in_the_target_generation() => _protectedTarget["defaulted"]!.GetValue<string>().ShouldNotEqual("personal default");
    [Fact] void should_release_the_default_value() => _released["defaulted"]!.GetValue<string>().ShouldEqual("personal default");
    [Fact] void should_erase_every_migrated_property() => _erased.All(property => property.Value!.GetValue<string>().Length == 0).ShouldBeTrue();
}
