// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class content_for_erasure_reporting : Specification
{
    protected InMemoryEncryptionKeyStorage _keys;
    protected bool _materialMissing;
    protected JsonSchemaMetadataManager _manager;
    protected JsonSchema _schema;
    protected JsonObject _plaintext;
    protected JsonObject _stored;
    protected string _storedText;
    protected (JsonObject Value, IReadOnlyList<string> ErasedPaths) _result;

    async Task Establish()
    {
        _keys = new();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        var access = Substitute.For<IEncryptionKeyStorage>();
        access.TryGetLatestFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(call => _materialMissing ? Task.FromResult<EncryptionKeyAtRevision?>(null) : _keys.TryGetLatestFor(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2)));
        access.GetErasureForRelease(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(call => _keys.GetErasureForRelease(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2)));
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, access, encryption),
                new EncryptedSubjectValueHandler(provisioner, access, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "profile":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}},
              "contacts":{"type":"array","items":{"type":"object","properties":{"email":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}},
              "scalars":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}},
              "nested":{"type":"array","items":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}},
              "dictionary":{"type":"object","additionalProperties":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}},
              "whole":{"type":"array","items":{"type":"string"},"compliance":[{"metadataType":"PII","details":""}]},
              "object":{"type":"object","properties":{"name":{"type":"string"}},"compliance":[{"metadataType":"PII","details":""}]},
              "count":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]},
              "flag":{"type":"boolean","compliance":[{"metadataType":"PII","details":""}]},
              "nullableNumber":{"type":["integer","null"],"compliance":[{"metadataType":"PII","details":""}]},
              "nullableText":{"type":["string","null"],"compliance":[{"metadataType":"PII","details":""}]},
              "nullableObject":{"type":["object","null"],"properties":{"name":{"type":"string"}},"compliance":[{"metadataType":"PII","details":""}]},
              "absent":{"type":["string","null"],"compliance":[{"metadataType":"PII","details":""}]},
              "location":{"type":"object","format":"point"},
              "protectedLocation":{"type":"object","format":"point","compliance":[{"metadataType":"PII","details":""}]},
              "secret":{"type":"string","security":[{"metadataType":"EncryptedSubject","details":""}]}
            }}
            """);
        _plaintext = JsonNode.Parse("""
            {"profile":{"name":"private"},"contacts":[{"email":"one"},{"email":"two"}],"scalars":["",null,"three"],"nested":[["four"],[]],"dictionary":{"home":"five"},"whole":["six"],"object":{"name":"seven"},"count":42,"flag":true,"nullableNumber":3,"nullableText":"eight","nullableObject":{"name":"nine"},"absent":null,"location":{"type":"Point","coordinates":[1,2]},"protectedLocation":{"type":"Point","coordinates":[3,4]},"secret":"confidential"}
            """).AsObject();
        _stored = await _manager.Apply("store", "tenant", _schema, "subject", _plaintext);
        _storedText = _stored.ToJsonString();
    }

    protected async Task Erase()
    {
        await _keys.RecordErasureFor("store", "tenant", "subject");
        await _keys.DeleteFor("store", "tenant", "subject");
    }

    protected async Task Release() => _result = await _manager.ReleaseReportingErasedProperties("store", "tenant", _schema, "subject", _stored);
}
