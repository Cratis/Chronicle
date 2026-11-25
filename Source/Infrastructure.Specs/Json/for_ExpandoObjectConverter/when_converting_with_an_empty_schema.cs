// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_with_an_empty_schema : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    JsonObject _json;
    ExpandoObject _fromJson;
    ExpandoObject _fromUtf8;
    string _written;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""{"type":"object"}""");
    }

    void Because()
    {
        dynamic source = new ExpandoObject();
        source.text = "hello";
        source.number = 42;
        source.flag = true;
        source.nothing = null;
        source.kind = DayOfWeek.Tuesday;
        source.map = new Dictionary<string, int> { ["a"] = 1 };
        source.list = new[] { "x", "y" };
        var expando = (ExpandoObject)source;

        _json = _converter.ToJsonObject(expando, _schema);
        _fromJson = _converter.ToExpandoObject(_json, _schema);
        _fromUtf8 = _converter.ToExpandoObject(Encoding.UTF8.GetBytes(_json.ToJsonString()), _schema);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            _converter.WriteJson(writer, expando, _schema);
        }

        _written = Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact] void should_keep_strings() => _json["text"]!.GetValue<string>().ShouldEqual("hello");
    [Fact] void should_keep_numbers() => _json["number"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_keep_booleans() => _json["flag"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_leave_out_nulls() => _json.ContainsKey("nothing").ShouldBeFalse();
    [Fact] void should_write_enums_as_numbers() => _json["kind"]!.GetValue<int>().ShouldEqual((int)DayOfWeek.Tuesday);
    [Fact] void should_write_dictionaries_as_objects() => _json["map"]!["a"]!.GetValue<int>().ShouldEqual(1);
    [Fact] void should_write_collections_as_arrays() => _json["list"]!.AsArray().Count.ShouldEqual(2);
    [Fact] void should_keep_the_clr_number_of_the_node_from_json() => ((IDictionary<string, object?>)_fromJson)["number"].ShouldEqual(42);
    [Fact] void should_read_arrays_from_json() => ((object[])((IDictionary<string, object?>)_fromJson)["list"]).Length.ShouldEqual(2);
    [Fact] void should_read_numbers_as_doubles_from_utf8() => ((IDictionary<string, object?>)_fromUtf8)["number"].ShouldEqual(42d);
    [Fact] void should_write_the_same_as_the_json_object() => _written.ShouldEqual(_json.ToJsonString());
}
