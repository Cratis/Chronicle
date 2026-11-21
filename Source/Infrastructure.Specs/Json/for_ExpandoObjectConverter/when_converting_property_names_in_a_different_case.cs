// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_property_names_in_a_different_case : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    JsonObject _json;
    ExpandoObject _expando;
    ExpandoObject _expandoWithBothCasings;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string"}}}""");
    }

    void Because()
    {
        dynamic source = new ExpandoObject();
        source.NAME = "upper";
        _json = _converter.ToJsonObject((ExpandoObject)source, _schema);
        _expando = _converter.ToExpandoObject(new JsonObject { ["Name"] = "mixed" }, _schema);

        dynamic both = new ExpandoObject();
        both.NAME = "upper";
        both.name = "exact";
        _expandoWithBothCasings = _converter.ToExpandoObject(
            _converter.ToJsonObject((ExpandoObject)both, _schema),
            _schema);
    }

    [Fact] void should_find_the_expando_property_ignoring_case() => _json["name"]!.GetValue<string>().ShouldEqual("upper");
    [Fact] void should_find_the_json_property_ignoring_case() => ((IDictionary<string, object?>)_expando)["name"].ShouldEqual("mixed");
    [Fact] void should_prefer_the_exact_casing() => ((IDictionary<string, object?>)_expandoWithBothCasings)["name"].ShouldEqual("exact");
}
