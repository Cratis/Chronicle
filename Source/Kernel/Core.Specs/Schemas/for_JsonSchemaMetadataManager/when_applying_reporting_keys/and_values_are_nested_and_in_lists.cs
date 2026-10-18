// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_applying_reporting_keys;

public class and_values_are_nested_and_in_lists : given.real_pii_protection
{
    (JsonObject Value, IReadOnlyList<ProtectedValueUse> Uses) _reported;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "nested":{"type":"object","properties":{"secret":{"type":"boolean"}}},
                "emails":{"type":"array","items":{"type":"string"}},
                "addresses":{"type":"array","items":{"type":"object","properties":{"street":{"type":"string"},"number":{"type":"integer"}}}},
                "matrix":{"type":"array","items":{"type":"array","items":{"type":"integer"}}}
            }}
            """);
        Mark(_schema.Properties["nested"].Properties["secret"]);
        Mark(_schema.Properties["emails"].Item!);
        Mark(_schema.Properties["addresses"].Item!);
        Mark(_schema.Properties["matrix"].Item!.Item!);
        _plaintext = JsonNode.Parse("""{"nested":{"secret":true},"emails":["a","b"],"addresses":[{"street":"Private Lane","number":42}],"matrix":[[1,2]]}""")!.AsObject();
    }

    async Task Because() => _reported = await _manager.ApplyReportingKeys("store", "tenant", _schema, "subject", _plaintext);

    [Fact] void should_report_canonical_dotted_and_indexed_paths() => string.Join(',', _reported.Uses.Select(_ => _.Path)).ShouldEqual("addresses[0].number,addresses[0].street,emails[0],emails[1],matrix[0][0],matrix[0][1],nested.secret");
    [Fact] void should_report_the_original_plaintext_at_each_boundary() => string.Join('|', _reported.Uses.Select(_ => _.Plaintext.ToJsonString())).ShouldEqual("42|\"Private Lane\"|\"a\"|\"b\"|1|2|true");
    [Fact] void should_report_the_key_used_for_every_value() => _reported.Uses.All(_ => _.Key.Kind == ProtectionKind.Pii && _.Key.Identifier.Value == "subject").ShouldBeTrue();
    [Fact] async Task should_preserve_the_existing_protection_shape() => JsonNode.DeepEquals(await _manager.TryRelease("store", "tenant", _schema, "subject", _reported.Value), _plaintext).ShouldBeTrue();
    [Fact] void should_not_mutate_the_input() => _plaintext["nested"]!["secret"]!.GetValue<bool>().ShouldBeTrue();
}
