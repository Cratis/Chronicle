// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_upcasting_protected_content;

public class and_the_migration_splits_combines_and_maps_pii : given.all_dependencies
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _targetSchema;
    JsonObject _released;
    ExpandoObjectConverter _converter;

    async Task Establish()
    {
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        _eventTypeMigrations = new(_storage, _converter);
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
                "mapped": {"type":"string","compliance":[{"metadataType":"PII","details":""}]}
              }
            }
            """);
        var upcast = JsonNode.Parse(
            """
            {
              "splitFirst":{"$split":{"source":"name","separator":" ","part":0}},
              "splitLast":{"$split":{"source":"name","separator":" ","part":1}},
              "combined":{"$combine":{"sources":["firstName","lastName"],"separator":" "}},
              "mapped":{"$mapValues":{"source":"status","mappings":[{"from":"old","to":"new"}]}}
            }
            """)!.AsObject();
        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.None,
            false,
            [new EventTypeGenerationDefinition(1, sourceSchema), new EventTypeGenerationDefinition(2, _targetSchema)],
            [new EventTypeMigrationDefinition(1, 2, [], upcast, new JsonObject())]));
        _content = await _manager.Apply(_eventStoreName, "test-namespace", sourceSchema, "author-subject", JsonNode.Parse(
            """{"name":"Jane Austen","firstName":"Jane","lastName":"Austen","status":"old"}""")!.AsObject());
        _contentAsExpandoObject = _converter.ToExpandoObject(_content, sourceSchema);
    }

    async Task Because()
    {
        var migrated = await _eventTypeMigrations.MigrateToAllGenerations(_eventStoreName, _eventType, _content, _contentAsExpandoObject);
        _released = await _manager.Release(
            _eventStoreName,
            "test-namespace",
            _targetSchema,
            "author-subject",
            _converter.ToJsonObject(migrated[(EventTypeGeneration)2], _targetSchema));
    }

    [Fact] void should_split_the_first_name_from_plaintext() => _released["splitFirst"]!.GetValue<string>().ShouldEqual("Jane");
    [Fact] void should_split_the_last_name_from_plaintext() => _released["splitLast"]!.GetValue<string>().ShouldEqual("Austen");
    [Fact] void should_combine_the_plaintext_names() => _released["combined"]!.GetValue<string>().ShouldEqual("Jane Austen");
    [Fact] void should_map_the_plaintext_value() => _released["mapped"]!.GetValue<string>().ShouldEqual("new");
}
