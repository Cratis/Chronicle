// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_converting_non_compliant_string_scalars.given;

public class a_converter_with_scalar_schema : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "flag": { "type": "boolean" },
                    "count": { "type": "integer" },
                    "status": {
                        "type": "integer",
                        "enum": [0, 1],
                        "x-enumNames": ["Unknown", "Verified"]
                    },
                    "rate": { "type": "number" }
                }
            }
            """);
    }

    protected ExpandoObject Convert(string propertyName, string value) =>
        _converter.ToExpandoObject(new JsonObject { [propertyName] = value }, _schema);
}
