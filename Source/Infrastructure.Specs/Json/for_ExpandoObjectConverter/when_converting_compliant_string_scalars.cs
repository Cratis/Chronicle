// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_compliant_string_scalars : Specification
{
    ExpandoObject _result;

    void Because()
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "flag": {
                        "type": "boolean",
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    },
                    "count": {
                        "type": "integer",
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    },
                    "rate": {
                        "type": "number",
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    }
                }
            }
            """);
        _result = converter.ToExpandoObject(
            new JsonObject
            {
                ["flag"] = "true",
                ["count"] = "42",
                ["rate"] = "12.5"
            },
            schema);
    }

    [Fact] void should_restore_the_boolean() => ((IDictionary<string, object?>)_result)["flag"].ShouldEqual(true);
    [Fact] void should_restore_the_integer() => ((IDictionary<string, object?>)_result)["count"].ShouldEqual(42);
    [Fact] void should_restore_the_number() => ((IDictionary<string, object?>)_result)["rate"].ShouldEqual(12.5d);
}
