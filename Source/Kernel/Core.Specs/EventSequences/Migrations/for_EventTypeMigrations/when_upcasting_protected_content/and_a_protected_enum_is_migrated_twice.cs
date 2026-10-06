// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_upcasting_protected_content;

public class and_a_protected_enum_is_migrated_twice : Specification
{
    ProtectedEventTypeMigrations _migrations;
    JsonSchemaMetadataManager _manager;
    protected ExpandoObjectConverter _converter;
    IEventTypesStorage _types;
    JsonSchema _firstSchema;
    JsonSchema _secondSchema;
    protected JsonSchema _thirdSchema;
    JsonObject _stored;
    protected JsonObject _protectedTarget;
    protected JsonObject _released;
    protected Exception _error;

    protected virtual bool UsesNumericCiphertext => false;

    async Task Establish()
    {
        _firstSchema = await Schema("Pending", 1);
        _secondSchema = await Schema("Verified", 2);
        _thirdSchema = await Schema("Accepted", 7);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        _manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new ExpandoObjectConverter(new TypeFormats());
        _types = Substitute.For<IEventTypesStorage>();
        _types.GetFor("status-event", 1).Returns(new EventTypeSchema(new EventType("status-event", 1), EventTypeOwner.Server, EventTypeSource.Code, _firstSchema));
        _types.GetFor("status-event", 2).Returns(new EventTypeSchema(new EventType("status-event", 2), EventTypeOwner.Server, EventTypeSource.Code, _secondSchema));
        _types.GetFor("status-event", 3).Returns(new EventTypeSchema(new EventType("status-event", 3), EventTypeOwner.Server, EventTypeSource.Code, _thirdSchema));
        var store = Substitute.For<IEventStoreStorage>();
        store.EventTypes.Returns(_types);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").Returns(store);
        _migrations = new ProtectedEventTypeMigrations(_types, new EventTypeMigrations(storage, _converter), _manager, _converter);
        _types.GetDefinition("status-event").Returns(Definition(false));
        var original = await _manager.Apply("store", "namespace", _firstSchema, "owner", new JsonObject { ["status"] = 1 });
        var first = await _migrations.Migrate("store", "namespace", new EventType("status-event", 1), original, _converter.ToExpandoObject(original, _firstSchema), "owner");
        _stored = UsesNumericCiphertext
            ? await _manager.Apply("store", "namespace", _secondSchema, "owner", new JsonObject { ["status"] = 2 })
            : _converter.ToJsonObject(first[2], _secondSchema);
        _types.GetDefinition("status-event").Returns(Definition(true));
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        var second = await _migrations.Migrate("store", "namespace", new EventType("status-event", 2), _stored, _converter.ToExpandoObject(_stored, _secondSchema), "owner");
        _protectedTarget = _converter.ToJsonObject(second[3], _thirdSchema);
        _released = await _manager.ReleaseStrict("store", "namespace", _thirdSchema, "owner", _protectedTarget);
    });

    static Task<JsonSchema> Schema(string name, int value) => JsonSchema.FromJsonAsync($$$$"""
        {"type":"object","properties":{"status":{"type":"integer","enum":[0,{{{{value}}}}],"x-enumNames":["Unknown","{{{{name}}}}"],"compliance":[{"metadataType":"PII","details":""}]}}}
        """);

    EventTypeDefinition Definition(bool third) => new(
        "status-event",
        EventTypeOwner.Server,
        false,
        third ? [new(1, _firstSchema), new(2, _secondSchema), new(3, _thirdSchema)] : [new(1, _firstSchema), new(2, _secondSchema)],
        third ? [Map(1, 2, 1, 2), Map(2, 3, 2, 7)] : [Map(1, 2, 1, 2)]);

    static EventTypeMigrationDefinition Map(uint fromGeneration, uint toGeneration, int from, int to) => new(
        fromGeneration,
        toGeneration,
        [],
        JsonNode.Parse($$$$"""
        {"status":{"$mapValues":{"source":"status","mappings":[{"from":{{{{from}}}},"to":{{{{to}}}}}]}}}
        """)!.AsObject(),
        new JsonObject());

    [Fact] void should_complete_the_second_migration() => _error.ShouldBeNull();
    [Fact] void should_map_the_enum_to_the_new_value() => ((IDictionary<string, object?>)_converter.ToExpandoObject(_released, _thirdSchema))["status"].ShouldEqual(7);
    [Fact] void should_protect_the_target_enum() => _protectedTarget["status"]!.GetValue<string>().ShouldNotEqual("Accepted");
}
