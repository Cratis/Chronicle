// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_classified_object_elements;

public class and_members_were_protected_individually : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _stored;
    JsonObject _released;
    JsonObject _erased;
    JsonObject _applied;
    InMemoryEncryptionKeyStorage _keys;

    async Task Establish()
    {
        _schema = JsonSchema.FromJson(
            """
            {
              "type": "object",
              "properties": {
                "items": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "compliance": [{ "metadataType": "PII", "details": "" }],
                    "properties": { "name": { "type": "string" }, "number": { "type": "integer" } }
                  }
                }
              }
            }
            """);
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        var handler = new PIICompliancePropertyValueHandler(provisioner, _keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        // This is the historical storage representation: protect members directly, not via the
        // array traversal under test, so an Apply/Release pair cannot hide a compatibility break.
        _stored = new JsonObject
        {
            ["items"] = new JsonArray(new JsonObject
            {
                ["name"] = await handler.Apply("test-store", "test-namespace", "owner", JsonValue.Create("Jane")),
                ["number"] = await handler.Apply("test-store", "test-namespace", "owner", JsonValue.Create(42))
            })
        };
    }

    async Task Because()
    {
        _released = await _manager.Release("test-store", "test-namespace", _schema, "owner", _stored);
        _applied = await _manager.Apply("test-store", "test-namespace", _schema, "owner", _released);
        await _keys.RecordErasureFor("test-store", "test-namespace", "owner");
        await _keys.DeleteFor("test-store", "test-namespace", "owner");
        _erased = await _manager.Release("test-store", "test-namespace", _schema, "owner", _stored);
    }

    [Fact] void should_release_the_legacy_string_member() => _released["items"]![0]!["name"]!.GetValue<string>().ShouldEqual("Jane");
    [Fact] void should_release_the_legacy_numeric_member() => _released["items"]![0]!["number"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_keep_the_object_shape_on_subsequent_writes() => (_applied["items"]![0] is JsonObject).ShouldBeTrue();
    [Fact] void should_protect_the_member_on_subsequent_writes() => _applied["items"]![0]!["name"]!.GetValue<string>().ShouldNotEqual("Jane");
    [Fact] void should_erase_the_legacy_string_member() => _erased["items"]![0]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_erase_the_legacy_numeric_member() => _erased["items"]![0]!["number"]!.GetValue<int>().ShouldEqual(0);
}
