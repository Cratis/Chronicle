// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schemas_for_a_byte_array_property : given.a_json_schema_generator
{
    class EventWithByteArray
    {
        public byte[] PhotoData { get; set; } = [];

        public string ContentEncoding { get; set; } = string.Empty;

        [JsonPropertyName("contentEncoding")]
        public string Encoding { get; set; } = string.Empty;
    }

    JsonObject _preciseProperties;
    JsonObject _legacyProperties;
    JsonObject _readModelProperties;

    void Because()
    {
        var type = typeof(EventWithByteArray);
        _preciseProperties = JsonNode.Parse(_generator.Generate(type).ToJson())!["properties"]!.AsObject();
        _legacyProperties = JsonNode.Parse(_generator.GenerateLegacyEventType(type).ToJson())!["properties"]!.AsObject();
        _readModelProperties = JsonNode.Parse(_generator.GenerateForReadModel(type).ToJson())!["properties"]!.AsObject();
    }

    [Fact] void should_keep_the_precise_byte_array_shape_from_net10() => _preciseProperties["PhotoData"]!.AsObject().ContainsKey("contentEncoding").ShouldBeFalse();
    [Fact] void should_keep_the_legacy_byte_array_shape_from_net10() => _legacyProperties["PhotoData"]!.AsObject().ContainsKey("contentEncoding").ShouldBeFalse();
    [Fact] void should_keep_the_read_model_byte_array_shape_from_net10() => _readModelProperties["PhotoData"]!.AsObject().ContainsKey("contentEncoding").ShouldBeFalse();
    [Fact] void should_keep_the_byte_array_format() => _preciseProperties["PhotoData"]!["format"]!.GetValue<string>().ShouldEqual("byte-array");
    [Fact] void should_preserve_a_payload_property_named_content_encoding() => _preciseProperties.ContainsKey("ContentEncoding").ShouldBeTrue();
    [Fact] void should_preserve_a_payload_property_with_the_exact_keyword_name() => _preciseProperties.ContainsKey("contentEncoding").ShouldBeTrue();
}
