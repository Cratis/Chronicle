// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_unresolved_contextual_references : Specification
{
    IDictionary<string, object?> _read;
    JsonObject _written;
    object? _dynamicScalar;
    object? _recursiveScalar;
    JsonNode? _dynamicJsonScalar;
    JsonNode? _recursiveJsonScalar;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
              "type":"object",
              "properties":{
                "dynamicValue":{"$dynamicRef":"https://unavailable.example/schema#value"},
                "recursiveValue":{"$recursiveRef":42}
              }
            }
            """);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var read = converter.ToExpandoObject(new JsonObject { ["dynamicValue"] = 42, ["recursiveValue"] = 17 }, schema);
        _read = read;
        _written = converter.ToJsonObject(read, schema);
        _dynamicScalar = SchemaScalarConverter.FromJsonValue(JsonValue.Create(42)!, schema.Properties["dynamicValue"]);
        _recursiveScalar = SchemaScalarConverter.FromJsonValue(JsonValue.Create(17)!, schema.Properties["recursiveValue"]);
        _dynamicJsonScalar = SchemaScalarConverter.ToJsonNode(_dynamicScalar, schema.Properties["dynamicValue"]);
        _recursiveJsonScalar = SchemaScalarConverter.ToJsonNode(_recursiveScalar, schema.Properties["recursiveValue"]);
    }

    [Fact] void should_read_the_unresolved_dynamic_value() => _read["dynamicValue"].ShouldEqual(42);
    [Fact] void should_read_the_invalid_recursive_value() => _read["recursiveValue"].ShouldEqual(17);
    [Fact] void should_write_the_unresolved_dynamic_value() => _written["dynamicValue"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_write_the_invalid_recursive_value() => _written["recursiveValue"]!.GetValue<int>().ShouldEqual(17);
    [Fact] void should_read_the_unresolved_dynamic_scalar() => _dynamicScalar.ShouldEqual(42);
    [Fact] void should_read_the_invalid_recursive_scalar() => _recursiveScalar.ShouldEqual(17);
    [Fact] void should_write_the_unresolved_dynamic_scalar() => _dynamicJsonScalar!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_write_the_invalid_recursive_scalar() => _recursiveJsonScalar!.GetValue<int>().ShouldEqual(17);
}
