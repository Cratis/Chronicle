// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_applying_reporting_keys;

public class and_metadata_has_no_handler : given.real_pii_protection
{
    (JsonObject Value, IReadOnlyList<ProtectedValueUse> Uses) _reported;
    IReadOnlyList<ProtectedValueLocation> _locations;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string"}}}""");
        _schema.Properties["value"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[] { new("unknown", string.Empty) }
        };
        _plaintext = new JsonObject { ["value"] = "ordinary" };
    }

    async Task Because()
    {
        _reported = await _manager.ApplyReportingKeys("store", "tenant", _schema, "subject", _plaintext);
        _locations = await _manager.LocateProtectedValues(_schema, "subject", _plaintext);
    }

    [Fact] void should_not_report_a_key_use() => _reported.Uses.ShouldBeEmpty();
    [Fact] void should_not_report_a_location() => _locations.ShouldBeEmpty();
    [Fact] void should_preserve_the_plain_content() => JsonNode.DeepEquals(_reported.Value, _plaintext).ShouldBeTrue();
}
