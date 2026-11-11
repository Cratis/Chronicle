// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schemas_for_an_obsolete_event : given.a_json_schema_generator
{
    [Obsolete("Used to verify obsolete event schema generation.")]
    record ObsoleteEvent(string Name);

    JsonObject _precise;
    JsonObject _legacy;
    JsonObject _readModel;

    void Because()
    {
        // Resolve the obsolete type without introducing an obsolete-use compiler warning.
        var type = GetType().GetNestedType("ObsoleteEvent", BindingFlags.NonPublic)!;
        _precise = JsonNode.Parse(_generator.Generate(type).ToJson())!.AsObject();
        _legacy = JsonNode.Parse(_generator.GenerateLegacyEventType(type).ToJson())!.AsObject();
        _readModel = JsonNode.Parse(_generator.GenerateForReadModel(type).ToJson())!.AsObject();
    }

    [Fact] void should_keep_the_precise_event_shape_from_net10() => _precise.ContainsKey("deprecated").ShouldBeFalse();
    [Fact] void should_keep_the_legacy_event_shape_from_net10() => _legacy.ContainsKey("deprecated").ShouldBeFalse();
    [Fact] void should_keep_the_read_model_shape_from_net10() => _readModel.ContainsKey("deprecated").ShouldBeFalse();
    [Fact] void should_preserve_the_event_payload() => _precise["properties"]!.AsObject().ContainsKey("Name").ShouldBeTrue();
}
