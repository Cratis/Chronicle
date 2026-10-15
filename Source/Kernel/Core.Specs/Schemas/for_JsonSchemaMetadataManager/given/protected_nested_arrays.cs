// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class protected_nested_arrays : real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"contacts":{"type":"array","items":{"type":"array","items":{"type":"string"}}}}}
            """);
        Mark(_schema.Properties["contacts"].Item!);
        _plaintext = JsonNode.Parse("""{"contacts":[["001","private@example.com"],[],[null,"{\"private\":true}"]]}""")!.AsObject();
        await Protect();
    }
}
