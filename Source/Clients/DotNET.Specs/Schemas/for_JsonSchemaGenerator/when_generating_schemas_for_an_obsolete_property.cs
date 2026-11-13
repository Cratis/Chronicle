// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schemas_for_an_obsolete_property : given.a_json_schema_generator
{
    class EventWithObsoleteProperty
    {
        [Obsolete("Used to verify obsolete property schema generation.")]
        public string Name { get; set; } = string.Empty;

        public string Deprecated { get; set; } = string.Empty;
    }

    JsonObject _preciseProperties;
    JsonObject _legacyProperties;
    JsonObject _readModelProperties;

    void Because()
    {
        var type = typeof(EventWithObsoleteProperty);
        _preciseProperties = JsonNode.Parse(_generator.Generate(type).ToJson())!["properties"]!.AsObject();
        _legacyProperties = JsonNode.Parse(_generator.GenerateLegacyEventType(type).ToJson())!["properties"]!.AsObject();
        _readModelProperties = JsonNode.Parse(_generator.GenerateForReadModel(type).ToJson())!["properties"]!.AsObject();
    }

    [Fact] void should_keep_the_precise_property_shape_from_net10() => _preciseProperties["Name"]!.AsObject().ContainsKey("deprecated").ShouldBeFalse();
    [Fact] void should_keep_the_legacy_property_shape_from_net10() => _legacyProperties["Name"]!.AsObject().ContainsKey("deprecated").ShouldBeFalse();
    [Fact] void should_keep_the_read_model_property_shape_from_net10() => _readModelProperties["Name"]!.AsObject().ContainsKey("deprecated").ShouldBeFalse();
    [Fact] void should_preserve_a_precise_payload_property_named_deprecated() => _preciseProperties.ContainsKey("Deprecated").ShouldBeTrue();
    [Fact] void should_preserve_a_legacy_payload_property_named_deprecated() => _legacyProperties.ContainsKey("Deprecated").ShouldBeTrue();
    [Fact] void should_preserve_a_read_model_payload_property_named_deprecated() => _readModelProperties.ContainsKey("Deprecated").ShouldBeTrue();
}
