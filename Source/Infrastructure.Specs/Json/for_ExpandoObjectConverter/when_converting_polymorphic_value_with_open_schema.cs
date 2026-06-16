// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_polymorphic_value_with_open_schema : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    JsonObject _result;

    void Establish()
    {
        _converter = new(new TypeFormats());

        // An open object schema (no fixed properties) is what the schema generator emits for a
        // polymorphic base type adorned with [DerivedType]. The converter must preserve the full
        // payload — including the derived type discriminator and every concrete subtype property.
        _schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "element": { "type": "object" }
                }
            }
            """);
    }

    void Because()
    {
        var input = new JsonObject
        {
            ["element"] = new JsonObject
            {
                ["_derivedTypeId"] = "externalComponent",
                ["componentName"] = "PrimeReact.Button",
                ["width"] = 120
            }
        };

        var expando = _converter.ToExpandoObject(input, _schema);
        _result = _converter.ToJsonObject(expando, _schema);
    }

    [Fact] void should_preserve_the_derived_type_discriminator() => _result["element"]!["_derivedTypeId"]!.GetValue<string>().ShouldEqual("externalComponent");
    [Fact] void should_preserve_concrete_subtype_properties() => _result["element"]!["componentName"]!.GetValue<string>().ShouldEqual("PrimeReact.Button");
    [Fact] void should_preserve_base_properties() => _result["element"]!["width"]!.GetValue<int>().ShouldEqual(120);
}
