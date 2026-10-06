// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_applying_an_erasure_fence;

public class and_values_share_a_subject : Specification
{
    IEncryptionKeyStorage _keys;
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _content;
    JsonObject _result;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "contacts":{"type":"array","items":{"type":"object","properties":{
                "email":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}
              }}}
            }}
            """);
        _content = JsonNode.Parse("""{"name":"owner","contacts":[{"email":"first"},{"email":"second"},{"email":"third"}]}""")!.AsObject();
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _keys.GetErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(new EncryptionKeyErasure(1, [], false));
        var encryption = new Encryption();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keys, encryption);
        _manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because() => _result = await _manager.ApplyErasureFence("store", "namespace", _schema, "owner", _content);

    [Fact] async Task should_resolve_erasure_only_once() => await _keys.Received(1).GetErasureFor("store", "namespace", "owner");
    [Fact] void should_erase_the_name() => _result["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_erase_every_child() => _result["contacts"]!.AsArray().All(child => child!["email"]!.GetValue<string>().Length == 0).ShouldBeTrue();
}
