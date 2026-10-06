// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_releasing_plaintext_scalar_arrays : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _original;
    JsonObject _released;
    JsonObject _strictlyReleased;
    Exception? _error;

    async Task Establish()
    {
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "names":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}},
              "numbers":{"type":"array","items":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]}}
            }}
            """);
        _original = JsonNode.Parse("""{"names":["Jane","Austen"],"numbers":[42,7]}""")!.AsObject();
    }

    async Task Because()
    {
        _released = await _manager.Release("store", "namespace", _schema, "owner", _original);
        _error = await Catch.Exception(async () => _strictlyReleased = await _manager.ReleaseStrict("store", "namespace", _schema, "owner", _original));
    }

    [Fact] void should_preserve_plaintext_on_release() => JsonNode.DeepEquals(_original, _released).ShouldBeTrue();
    [Fact] void should_not_fail_strict_release() => _error.ShouldBeNull();
    [Fact] void should_preserve_plaintext_on_strict_release() => JsonNode.DeepEquals(_original, _strictlyReleased).ShouldBeTrue();
}
