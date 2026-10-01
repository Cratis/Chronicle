// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_round_tripping_nullable_empty_strings : Specification
{
    const string Subject = "nullable-empty-strings";
    JsonObject _stored;
    JsonObject _released;
    JsonObject _passThrough;
    JsonObject _erased;
    InMemoryEncryptionKeyStorage _keys;
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _input;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "nickname": { "type": ["string", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
                "aliases": { "type": "array", "items": { "type": ["string", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] } },
                "subjectSecret": { "type": ["string", "null"], "security": [{ "metadataType": "EncryptedSubject", "details": "" }] },
                "namespaceSecret": { "type": ["string", "null"], "security": [{ "metadataType": "EncryptedNamespace", "details": "" }] },
                "globalSecret": { "type": ["string", "null"], "security": [{ "metadataType": "EncryptedGlobal", "details": "" }] },
                "secrets": { "type": "array", "items": { "type": ["string", "null"], "security": [{ "metadataType": "EncryptedSubject", "details": "" }] } }
              }
            }
            """);
        _input = JsonNode.Parse("""{ "nickname": "", "aliases": ["", null], "subjectSecret": "", "namespaceSecret": "", "globalSecret": "", "secrets": ["", null] }""")!.AsObject();
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, _keys, encryption),
                new EncryptedSubjectValueHandler(provisioner, _keys, encryption),
                new EncryptedNamespaceValueHandler(provisioner, _keys, encryption),
                new EncryptedGlobalValueHandler(provisioner, _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because()
    {
        _stored = await _manager.ApplyToReadModel("store", "Default", _schema, Subject, _input);
        _released = await _manager.Release("store", "Default", _schema, Subject, _stored);
        _passThrough = await _manager.Release("store", "Default", _schema, Subject, _input);
        await _keys.RecordErasureFor("store", "Default", Subject);
        await _keys.DeleteFor("store", "Default", Subject);
        _erased = await _manager.Release("store", "Default", _schema, Subject, _stored);
    }

    [Fact] void should_encrypt_the_empty_nickname() => ProtectedValueCodec.TryDecodeCipherText(new Encryption(), _stored["nickname"]!.GetValue<string>(), out _).ShouldBeTrue();
    [Fact] void should_preserve_the_live_empty_nickname() => _released["nickname"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_live_empty_collection_element() => _released["aliases"]![0]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_live_null_collection_element() => _released["aliases"]![1].ShouldBeNull();
    [Fact] void should_preserve_the_empty_subject_secret() => _released["subjectSecret"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_empty_namespace_secret() => _released["namespaceSecret"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_empty_global_secret() => _released["globalSecret"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_empty_encrypted_collection_element() => _released["secrets"]![0]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_pass_through_the_unencrypted_empty_nickname() => _passThrough["nickname"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_pass_through_the_unencrypted_empty_collection_element() => _passThrough["aliases"]![0]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_release_the_erased_nickname_as_null() => _erased["nickname"].ShouldBeNull();
    [Fact] void should_release_the_erased_collection_element_as_null() => _erased["aliases"]![0].ShouldBeNull();
    [Fact] void should_leave_the_security_key_unaffected_by_pii_erasure() => _erased["subjectSecret"]!.GetValue<string>().ShouldEqual(string.Empty);
}
