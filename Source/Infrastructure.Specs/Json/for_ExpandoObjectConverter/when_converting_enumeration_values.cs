// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_enumeration_values : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    JsonObject _known;
    JsonObject _unknown;
    ExpandoObject _read;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "status": { "type": "integer", "enum": [0, 5], "x-enumNames": ["Unknown", "Verified"] }
                }
            }
            """);
    }

    void Because()
    {
        dynamic known = new ExpandoObject();
        known.status = 5;
        dynamic unknown = new ExpandoObject();
        unknown.status = 9;
        _known = _converter.ToJsonObject((ExpandoObject)known, _schema);
        _unknown = _converter.ToJsonObject((ExpandoObject)unknown, _schema);
        _read = _converter.ToExpandoObject(new JsonObject { ["status"] = "Verified" }, _schema);
    }

    [Fact] void should_write_the_name_of_a_known_value() => _known["status"]!.GetValue<string>().ShouldEqual("Verified");
    [Fact] void should_fall_back_to_the_default_for_an_unknown_value() => _unknown["status"]!.GetValue<string>().ShouldEqual("Unknown");
    [Fact] void should_read_the_value_of_a_name() => ((IDictionary<string, object?>)_read)["status"].ShouldEqual(5);
}
