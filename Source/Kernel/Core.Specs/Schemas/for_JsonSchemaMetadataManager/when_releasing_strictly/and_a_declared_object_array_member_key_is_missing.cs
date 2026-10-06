// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_strictly;

public class and_a_declared_object_array_member_key_is_missing : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _content;
    Exception _error;
    string _original;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"contacts":{"type":"array","items":{"type":"object","properties":{
              "email":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}
            }}}}}
            """);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        _manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        _content = await _manager.Apply("store", "namespace", _schema, "owner", JsonNode.Parse("""{"contacts":[{"email":"personal@example.com"}]}""")!.AsObject());
        _original = _content.ToJsonString();
        await keys.DeleteFor("store", "namespace", "owner");
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.ReleaseStrict("store", "namespace", _schema, "owner", _content));

    [Fact] void should_fail_the_entire_release() => _error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_leave_the_input_unchanged() => _content.ToJsonString().ShouldEqual(_original);
}
