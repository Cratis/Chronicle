// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_an_array_has_no_item_schema : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"secret":{"type":"string"},"values":{"type":"array"}}}""");
        Mark(_schema.Properties["secret"]);
        _plaintext = JsonNode.Parse("""{"secret":"private","values":[1,"text",null,{"arbitrary":true},[2]]}""")!.AsObject();
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_leave_the_unprotected_array_unchanged() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
}
