// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_serializing_a_normal_list : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _input;
    JsonObject _json;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "scores": {
                        "type": "array",
                        "items": { "type": "object", "properties": { "value": { "type": "integer" } } }
                    }
                }
            }
            """);

        // A real list value (not a blob-encrypted string) must still serialize as a JSON array — the
        // scalar-string guard for coarse [PII] lists must not over-trigger and flatten genuine arrays.
        var element = new ExpandoObject();
        ((IDictionary<string, object?>)element)["value"] = 5;
        _input = new ExpandoObject();
        ((IDictionary<string, object?>)_input)["scores"] = new object[] { element };
    }

    void Because() => _json = _converter.ToJsonObject(_input, _schema);

    [Fact] void should_serialize_the_list_as_an_array() => (_json["scores"] is JsonArray).ShouldBeTrue();

    [Fact] void should_preserve_the_element() => _json["scores"]![0]!["value"]!.GetValue<int>().ShouldEqual(5);
}
