// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schemas_for_a_byte_array_property : given.a_json_schema_generator
{
    class EventWithByteArrayProperty
    {
        public byte[] PhotoData { get; set; } = [];

        public string ContentEncoding { get; set; } = string.Empty;
    }

    JsonObject _preciseProperties;
    JsonObject _legacyProperties;
    JsonObject _readModelProperties;

    void Because()
    {
        var type = typeof(EventWithByteArrayProperty);
        _preciseProperties = JsonNode.Parse(_generator.Generate(type).ToJson())!["properties"]!.AsObject();
        _legacyProperties = JsonNode.Parse(_generator.GenerateLegacyEventType(type).ToJson())!["properties"]!.AsObject();
        _readModelProperties = JsonNode.Parse(_generator.GenerateForReadModel(type).ToJson())!["properties"]!.AsObject();
    }

    [Fact] void should_keep_the_precise_property_shape_from_net10() => _preciseProperties["PhotoData"]!.AsObject().ContainsKey("contentEncoding").ShouldBeFalse();
    [Fact] void should_keep_the_legacy_property_shape_from_net10() => _legacyProperties["PhotoData"]!.AsObject().ContainsKey("contentEncoding").ShouldBeFalse();
    [Fact] void should_keep_the_read_model_property_shape_from_net10() => _readModelProperties["PhotoData"]!.AsObject().ContainsKey("contentEncoding").ShouldBeFalse();
    [Fact] void should_preserve_a_precise_payload_property_named_content_encoding() => _preciseProperties.ContainsKey("ContentEncoding").ShouldBeTrue();
    [Fact] void should_preserve_a_legacy_payload_property_named_content_encoding() => _legacyProperties.ContainsKey("ContentEncoding").ShouldBeTrue();
    [Fact] void should_preserve_a_read_model_payload_property_named_content_encoding() => _readModelProperties.ContainsKey("ContentEncoding").ShouldBeTrue();
}
