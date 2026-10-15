// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_an_ordinary_scalar_array_is_present : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"secret":{"type":"string"},"labels":{"type":"array","items":{"type":"string"}}}}""");
        Mark(_schema.Properties["secret"]);
        _plaintext = JsonNode.Parse("""{"secret":"private","labels":["first","second"]}""")!.AsObject();
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_release_without_reparenting_ordinary_elements() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
}
