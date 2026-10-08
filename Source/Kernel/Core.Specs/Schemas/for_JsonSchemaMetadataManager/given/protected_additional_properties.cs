// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class protected_additional_properties : real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"contacts":{"type":"object","additionalProperties":{"type":"string"}}}}
            """);
        Mark(_schema.Properties["contacts"].AdditionalPropertiesSchema!);
        _plaintext = JsonNode.Parse("""{"contacts":{"home":"001","work":"private@example.com"}}""")!.AsObject();
        await Protect();
    }
}
