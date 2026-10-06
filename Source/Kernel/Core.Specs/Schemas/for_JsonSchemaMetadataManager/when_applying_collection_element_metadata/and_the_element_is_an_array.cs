// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_applying_collection_element_metadata;

public class and_the_element_is_an_array : Specification
{
    const string Subject = "owner";
    const string Plaintext = "classified-value";

    JsonSchema _schema;
    JsonObject _input;
    JsonObject _applied;
    JsonObject _released;
    JsonObject _releasedAfterErasure;
    Exception? _subsequentWriteFailure;
    InMemoryEncryptionKeyStorage _keyStorage;
    JsonSchemaMetadataManager _manager;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "names": {
                  "type": "array",
                  "items": { "type": "array", "compliance": [{ "metadataType": "PII", "details": "" }], "items": { "type": "string" } }
                },
                "secrets": {
                  "type": "array",
                  "items": { "type": "array", "security": [{ "metadataType": "EncryptedNamespace", "details": "" }], "items": { "type": "string" } }
                }
              }
            }
            """);
        _input = new JsonObject
        {
            ["names"] = new JsonArray(new JsonArray(Plaintext)),
            ["secrets"] = new JsonArray(new JsonArray(Plaintext))
        };
        _keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keyStorage, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, _keyStorage, encryption),
                new EncryptedNamespaceValueHandler(provisioner, _keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because()
    {
        _applied = await _manager.Apply("test-store", "test-namespace", _schema, Subject, _input);
        _released = await _manager.Release("test-store", "test-namespace", _schema, Subject, _applied);
        await _keyStorage.DeleteFor("test-store", "test-namespace", Subject);
        _releasedAfterErasure = await _manager.Release("test-store", "test-namespace", _schema, Subject, _applied);
        _subsequentWriteFailure = await Catch.Exception(() => _manager.Apply("test-store", "test-namespace", _schema, Subject, _input));
    }

    [Fact] void should_encrypt_the_personal_element_as_a_whole() => (_applied["names"]![0] is JsonValue).ShouldBeTrue();
    [Fact] void should_encrypt_the_confidential_element_as_a_whole() => (_applied["secrets"]![0] is JsonValue).ShouldBeTrue();
    [Fact] void should_restore_the_container_shape_on_release() => _released.ToJsonString().ShouldEqual(_input.ToJsonString());
    [Fact] void should_erase_the_personal_array_element() => _releasedAfterErasure["names"]![0]!.AsArray().Count.ShouldEqual(0);
    [Fact] void should_keep_namespace_confidentiality_readable_after_erasure() => _releasedAfterErasure["secrets"]![0]![0]!.GetValue<string>().ShouldEqual(Plaintext);
    [Fact] void should_refuse_new_personal_array_elements_after_erasure() => _subsequentWriteFailure.ShouldBeOfExactType<SchemaMetadataActionFailed>();
}
