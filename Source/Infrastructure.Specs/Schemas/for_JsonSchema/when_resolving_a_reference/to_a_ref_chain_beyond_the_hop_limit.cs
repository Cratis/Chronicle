// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_a_ref_chain_beyond_the_hop_limit : Specification
{
    JsonSchema _schema;
    JsonSchema _actual;

    void Establish()
    {
        var definitions = new JsonObject();
        for (var index = 0; index < 66; index++)
        {
            definitions[$"hop{index}"] = new JsonObject { ["title"] = $"hop{index}", ["$ref"] = $"#/$defs/hop{index + 1}" };
        }
        definitions["hop66"] = JsonNode.Parse("""{"type":"string","compliance":[{"metadataType":"PII","details":""}]}""");
        _schema = new JsonSchema(new JsonObject { ["$ref"] = "#/$defs/hop0", ["$defs"] = definitions });
    }

    void Because() => _actual = _schema.ActualSchema;

    [Fact] void should_stop_after_sixty_four_reference_hops() => _actual.Title.ShouldEqual("hop63");
    [Fact] void should_not_walk_metadata_past_the_hop_limit() => _schema.HasComplianceMetadata().ShouldBeFalse();
}
