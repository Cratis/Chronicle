// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_round_tripping_protected_scalars;

public class and_recursive_references_supply_the_scalar_kinds : given.real_pii_protection
{
    StatusedRelease _statused;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {
              "$id":"https://schemas.example/scalars", "type":"object",
              "properties":{
                "text":{"$ref":"text#/$defs/value"},
                "integer":{"$ref":"integer#/$defs/value"},
                "boolean":{"$ref":"boolean#/$defs/value"}
              },
              "$defs":{
                "text":{"$id":"text","$recursiveAnchor":true,"type":"string","compliance":[{"metadataType":"PII","details":""}],"$defs":{"value":{"$recursiveRef":"#"}}},
                "integer":{"$id":"integer","$recursiveAnchor":true,"type":"integer","compliance":[{"metadataType":"PII","details":""}],"$defs":{"value":{"$recursiveRef":"#"}}},
                "boolean":{"$id":"boolean","$recursiveAnchor":true,"type":"boolean","compliance":[{"metadataType":"PII","details":""}],"$defs":{"value":{"$recursiveRef":"#"}}}
              }
            }
            """);
        _plaintext = JsonNode.Parse("""{"text":"42","integer":42,"boolean":true}""")!.AsObject();
        await Protect();
    }

    async Task Because()
    {
        _result = await _manager.Release("store", "tenant", _schema, "subject", _protected);
        _statused = await _manager.ReleaseWithStatus("store", "tenant", _schema, "subject", _protected);
    }

    [Fact] void should_restore_all_scalar_kinds_and_values() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_restore_all_scalar_kinds_and_values_with_status() => JsonNode.DeepEquals(_statused.Content, _plaintext).ShouldBeTrue();
    [Fact] void should_report_decryption_for_each_boundary() => _statused.Statuses.Values.ShouldEachConformTo(status => status == ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_report_all_three_boundaries() => _statused.Statuses.Count.ShouldEqual(3);
    [Fact] void should_encrypt_every_scalar() => _protected.All(property => property.Value!.GetValue<string>() != _plaintext[property.Key]!.ToString()).ShouldBeTrue();
    [Fact] void should_restore_kinds_not_declared_at_the_recursive_boundaries() => _schema.Properties.Values.All(property => property.Reference!.Type == JsonObjectType.None).ShouldBeTrue();
}
