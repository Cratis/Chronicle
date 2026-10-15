// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_multiple_metadata_entries_apply : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string"}}} """);
        _schema.Properties["value"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[]
            {
                new(ComplianceMetadataType.PII.Value, string.Empty),
                new("another-protection", string.Empty)
            }
        };
        _plaintext = new JsonObject { ["value"] = "private" };
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_not_accept_a_partially_released_value() => _result.ShouldBeNull();
}
