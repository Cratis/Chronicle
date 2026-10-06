// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_applying_dictionary_value_metadata;

public class and_no_other_property_has_metadata : Specification
{
    const string Subject = "owner";
    const string Plaintext = "classified-value";

    JsonSchema _schema;
    JsonObject _input;
    JsonObject _applied;
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
                  "type": "object",
                  "additionalProperties": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] }
                },
                "secrets": {
                  "type": "object",
                  "additionalProperties": { "type": "string", "security": [{ "metadataType": "EncryptedNamespace", "details": "" }] }
                }
              }
            }
            """);
        _input = new JsonObject
        {
            ["names"] = new JsonObject { ["home"] = Plaintext },
            ["secrets"] = new JsonObject { ["home"] = Plaintext }
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
        await _keyStorage.DeleteFor("test-store", "test-namespace", Subject);
        _releasedAfterErasure = await _manager.Release("test-store", "test-namespace", _schema, Subject, _applied);
        _subsequentWriteFailure = await Catch.Exception(() => _manager.Apply("test-store", "test-namespace", _schema, Subject, _input));
    }

    [Fact] void should_detect_dictionary_value_compliance() => _schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance).ShouldBeTrue();
    [Fact] void should_detect_dictionary_value_confidentiality() => _schema.HasSchemaMetadata(SchemaMetadataCategory.Security).ShouldBeTrue();
    [Fact] void should_encrypt_the_personal_dictionary_value() => _applied["names"]!["home"]!.GetValue<string>().ShouldNotEqual(Plaintext);
    [Fact] void should_encrypt_the_confidential_dictionary_value() => _applied["secrets"]!["home"]!.GetValue<string>().ShouldNotEqual(Plaintext);
    [Fact] void should_erase_the_personal_dictionary_value() => _releasedAfterErasure["names"]!["home"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_keep_namespace_confidentiality_readable_after_erasure() => _releasedAfterErasure["secrets"]!["home"]!.GetValue<string>().ShouldEqual(Plaintext);
    [Fact] void should_refuse_new_personal_dictionary_values_after_erasure() => _subsequentWriteFailure.ShouldBeOfExactType<SchemaMetadataActionFailed>();
}
